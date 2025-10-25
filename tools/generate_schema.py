#!/usr/bin/env python3
"""
TL Schema to C# Code Generator
Parses lite_api.tl and generates type-safe C# code
"""

import re
import zlib
from typing import List, Dict, Tuple, Optional
from dataclasses import dataclass
import urllib.request

LITE_API_URL = "https://raw.githubusercontent.com/ton-blockchain/ton/master/tl/generate/scheme/lite_api.tl"

@dataclass
class TLField:
    name: str
    type: str
    is_optional: bool = False
    condition: Optional[str] = None  # e.g., "mode.0" for conditional fields

@dataclass
class TLType:
    name: str
    fields: List[TLField]
    result_type: str
    is_function: bool = False
    constructor: int = 0

def compute_crc32(text: str) -> int:
    """Compute CRC32 of TL schema line"""
    return zlib.crc32(text.encode('utf-8')) & 0xFFFFFFFF

def parse_type(type_str: str) -> Tuple[str, bool]:
    """Parse TL type string to C# type"""
    is_optional = type_str.startswith('?')
    type_str = type_str.lstrip('?').strip()
    
    type_map = {
        'int': 'int',
        'long': 'long',
        'double': 'double',
        'string': 'string',
        'bytes': 'byte[]',
        'Bool': 'bool',
        'true': 'bool',
        'int256': 'byte[]',  # 32 bytes
        'int128': 'byte[]',  # 16 bytes
        '#': 'uint',
    }
    
    # Handle vectors - both "vector type" and "(vector type)"
    vector_match = re.match(r'vector\s+(.+)', type_str, re.IGNORECASE)
    if vector_match:
        inner_type_str = vector_match.group(1).strip()
        inner_type, _ = parse_type(inner_type_str)
        return f'{inner_type}[]', is_optional
    
    # Handle custom types (convert to PascalCase)
    if '.' in type_str:
        return to_pascal_case(type_str), is_optional
    
    # Handle result types (these might be union types)
    cs_type = type_map.get(type_str, to_pascal_case(type_str))
    return cs_type, is_optional

def parse_field(field_str: str) -> Optional[TLField]:
    """Parse a single TL field"""
    # Skip empty or comment fields
    if not field_str or field_str.startswith('//'):
        return None
    
    # Handle conditional fields: name:condition?type
    condition_match = re.match(r'(\w+):(\w+\.\d+)\?(.+)', field_str)
    if condition_match:
        name, condition, type_str = condition_match.groups()
        # Remove outer parentheses if present
        type_str = type_str.strip()
        if type_str.startswith('(') and type_str.endswith(')'):
            type_str = type_str[1:-1]
        cs_type, _ = parse_type(type_str)
        return TLField(name=name, type=cs_type, is_optional=True, condition=condition)
    
    # Handle regular fields: name:type or name:(type)
    field_match = re.match(r'(\w+):(.+)', field_str)
    if field_match:
        name, type_str = field_match.groups()
        # Remove outer parentheses if present
        type_str = type_str.strip()
        if type_str.startswith('(') and type_str.endswith(')'):
            type_str = type_str[1:-1]
        cs_type, is_optional = parse_type(type_str)
        return TLField(name=name, type=cs_type, is_optional=is_optional)
    
    return None

def parse_tl_line(line: str, is_function: bool = False) -> Optional[TLType]:
    """Parse a single TL definition line"""
    # Remove comments and extra spaces
    line = re.sub(r'//.*', '', line).strip()
    if not line or line.startswith('---'):
        return None
    
    # Split into definition and result type
    if '=' not in line:
        return None
    
    definition, result_type = line.split('=')
    definition = definition.strip()
    result_type = result_type.strip().rstrip(';')
    
    # Parse name and fields
    # Split carefully to preserve parenthesized groups
    # e.g., "name id:type field:(vector inner.type)" should split into ["name", "id:type", "field:(vector inner.type)"]
    field_parts = []
    current = ""
    paren_depth = 0
    
    for char in definition:
        if char == '(':
            paren_depth += 1
            current += char
        elif char == ')':
            paren_depth -= 1
            current += char
        elif char.isspace() and paren_depth == 0:
            if current:
                field_parts.append(current)
                current = ""
        else:
            current += char
    
    if current:
        field_parts.append(current)
    
    if not field_parts:
        return None
    
    # Extract name and strip explicit constructor hash (#hash)
    name_with_hash = field_parts[0]
    name = name_with_hash.split('#')[0]  # Strip #b12f65af etc
    field_parts = field_parts[1:]
    
    fields = []
    for field_str in field_parts:
        field = parse_field(field_str)
        if field:
            fields.append(field)
    
    # Compute constructor CRC32
    constructor = compute_crc32(line)
    
    return TLType(
        name=name,
        fields=fields,
        result_type=result_type,
        is_function=is_function,
        constructor=constructor
    )

def to_pascal_case(name: str) -> str:
    """Convert snake_case or lowerCamelCase to PascalCase"""
    # Types that should keep their LiteServer prefix to avoid conflicts
    keep_prefix_types = {'liteserver.version', 'liteserver.signature', 'liteserver.signatureset'}
    
    # Handle dots (e.g., liteServer.error -> Error, tonNode.blockId -> BlockId)
    if '.' in name:
        parts = name.split('.')
        # For tonNode and liteServer, strip only the first part (prefix)
        # but keep any nested parts like liteServer.nonfinal.candidate -> NonfinalCandidate
        if parts[0] in ('tonNode', 'liteServer'):
            # Check if we should keep the prefix
            if name.lower() in keep_prefix_types:
                # Keep the liteServer prefix
                result_parts = []
                for part in parts:
                    words = re.findall(r'[A-Z]+(?=[A-Z][a-z]|\b)|[A-Z][a-z]+|[a-z]+|[0-9]+', part)
                    result_parts.extend(words)
                return ''.join(w.capitalize() for w in result_parts)
            else:
                parts = parts[1:]  # Remove prefix
                # Now join the remaining parts
                result_parts = []
                for part in parts:
                    words = re.findall(r'[A-Z]+(?=[A-Z][a-z]|\b)|[A-Z][a-z]+|[a-z]+|[0-9]+', part)
                    result_parts.extend(words)
                return ''.join(w.capitalize() for w in result_parts)
        else:
            # For other prefixes, capitalize each part
            result_parts = []
            for part in parts:
                words = re.findall(r'[A-Z]+(?=[A-Z][a-z]|\b)|[A-Z][a-z]+|[a-z]+|[0-9]+', part)
                result_parts.extend(words)
            return ''.join(w.capitalize() for w in result_parts)
    
    # Handle underscores first (snake_case)
    if '_' in name or '-' in name:
        parts = name.replace('-', '_').split('_')
        return ''.join(p.capitalize() for p in parts if p)
    
    # Split camelCase and PascalCase
    # This regex handles: camelCase, PascalCase, numbers
    words = re.findall(r'[A-Z]+(?=[A-Z][a-z]|\b)|[A-Z][a-z]+|[a-z]+|[0-9]+', name)
    
    return ''.join(w.capitalize() for w in words)

def to_camel_case(name: str) -> str:
    """Convert snake_case to camelCase"""
    # First handle underscores
    parts = name.replace('-', '_').split('_')
    if len(parts) > 1:
        return parts[0].lower() + ''.join(p.capitalize() for p in parts[1:])
    
    # Then handle camelCase splitting
    words = re.findall(r'[A-Z]+(?=[A-Z][a-z]|\b)|[A-Z][a-z]+|[a-z]+|[0-9]+', name)
    if not words:
        return name
    return words[0].lower() + ''.join(w.capitalize() for w in words[1:])

def generate_field_declaration(field: TLField) -> str:
    """Generate C# field declaration"""
    cs_type = field.type
    if field.is_optional and not cs_type.endswith('?') and cs_type not in ['string', 'byte[]'] and not cs_type.endswith('[]'):
        cs_type = f'{cs_type}?'
    
    prop_name = to_pascal_case(field.name)
    
    # Handle special cases for default values
    if cs_type == 'byte[]':
        return f"public {cs_type} {prop_name} {{ get; set; }} = Array.Empty<byte>();"
    elif cs_type.endswith('[]') and not cs_type.startswith('byte'):
        inner = cs_type[:-2]
        return f"public {cs_type} {prop_name} {{ get; set; }} = Array.Empty<{inner}>();"
    elif cs_type == 'string':
        return f'public {cs_type} {prop_name} {{ get; set; }} = string.Empty;'
    else:
        return f'public {cs_type} {prop_name} {{ get; set; }}'

def generate_struct_or_class(tl_type: TLType, is_struct: bool = False, union_types: dict = None) -> str:
    """Generate C# struct or class for a TL type"""
    class_name = to_pascal_case(tl_type.name)
    keyword = 'struct' if is_struct else 'class'
    readonly = 'readonly ' if is_struct else ''
    
    # Check if this type is part of a union (has a base class)
    base_class = None
    if not is_struct and union_types and tl_type.result_type and tl_type.result_type in union_types:
        if len(union_types[tl_type.result_type]) > 1:
            base_class = to_pascal_case(tl_type.result_type)
    
    lines = []
    lines.append(f'/// <summary>')
    lines.append(f'/// {tl_type.name} = {tl_type.result_type}')
    if base_class:
        lines.append(f'/// Inherits from: {base_class}')
    lines.append(f'/// </summary>')
    
    if base_class:
        lines.append(f'public {keyword} {class_name} : {base_class}')
    else:
        lines.append(f'public {readonly}{keyword} {class_name}')
    
    lines.append('{')
    
    if not is_struct:
        if base_class:
            # Override abstract property
            lines.append(f'    public override uint Constructor => 0x{tl_type.constructor:08X};')
        else:
            lines.append(f'    public const uint Constructor = 0x{tl_type.constructor:08X};')
        lines.append('')
    
    # Generate fields/properties
    for field in tl_type.fields:
        if is_struct:
            cs_type = field.type
            prop_name = to_pascal_case(field.name)
            lines.append(f'    public readonly {cs_type} {prop_name};')
        else:
            lines.append(f'    {generate_field_declaration(field)}')
    
    if is_struct and tl_type.fields:
        lines.append('')
        # Generate constructor for struct
        params = ', '.join(f'{f.type} {to_camel_case(f.name)}' for f in tl_type.fields)
        lines.append(f'    public {class_name}({params})')
        lines.append('    {')
        for field in tl_type.fields:
            prop_name = to_pascal_case(field.name)
            param_name = to_camel_case(field.name)
            # Add validation for byte arrays
            if field.type == 'byte[]' and 'int256' in tl_type.name.lower():
                lines.append(f'        if ({param_name}.Length != 32) throw new ArgumentException("{prop_name} must be 32 bytes", nameof({param_name}));')
            lines.append(f'        {prop_name} = {param_name};')
        lines.append('    }')
    
    # Generate WriteTo method
    if tl_type.fields:
        lines.append('')
        # Use override if inheriting from abstract base
        write_modifier = 'override' if base_class else ''
        write_modifier_str = f'public {write_modifier} void WriteTo(TLWriteBuffer writer)'.strip()
        lines.append(f'    {write_modifier_str}')
        lines.append('    {')
        for field in tl_type.fields:
            prop_name = to_pascal_case(field.name)
            write_method = get_write_method(field.type, prop_name, field.name)
            if field.is_optional and field.condition:
                # Handle conditional writes (e.g., mode.0?field means write if bit 0 of mode is set)
                condition_match = re.match(r'(\w+)\.(\d+)', field.condition)
                if condition_match:
                    mode_field, bit = condition_match.groups()
                    mode_prop = to_pascal_case(mode_field)
                    lines.append(f'        if (({mode_prop} & (1u << {bit})) != 0)')
                    lines.append(f'        {{')
                    lines.append(f'            {write_method}')
                    lines.append(f'        }}')
                else:
                    lines.append(f'        {write_method}')
            else:
                lines.append(f'        {write_method}')
        lines.append('    }')
    
    # Generate ReadFrom method
    if tl_type.fields:
        lines.append('')
        lines.append(f'    public static {class_name} ReadFrom(TLReadBuffer reader)')
        lines.append('    {')
        if is_struct:
            lines.append('        return new ' + class_name + '(')
            read_statements = []
            for field in tl_type.fields:
                read_method = get_read_method(field.type, field.name)
                read_statements.append(f'            {read_method}')
            lines.append(',\n'.join(read_statements))
            lines.append('        );')
        else:
            # For classes with conditional fields, need to read mode first
            has_conditional = any(f.is_optional and f.condition for f in tl_type.fields)
            if has_conditional:
                lines.append(f'        var result = new {class_name}();')
                for field in tl_type.fields:
                    prop_name = to_pascal_case(field.name)
                    read_method = get_read_method(field.type, field.name)
                    if field.is_optional and field.condition:
                        condition_match = re.match(r'(\w+)\.(\d+)', field.condition)
                        if condition_match:
                            mode_field, bit = condition_match.groups()
                            mode_prop = to_pascal_case(mode_field)
                            lines.append(f'        if ((result.{mode_prop} & (1u << {bit})) != 0)')
                            lines.append(f'            result.{prop_name} = {read_method};')
                        else:
                            lines.append(f'        result.{prop_name} = {read_method};')
                    else:
                        lines.append(f'        result.{prop_name} = {read_method};')
                lines.append('        return result;')
            else:
                lines.append(f'        return new {class_name}')
                lines.append('        {')
                for field in tl_type.fields:
                    prop_name = to_pascal_case(field.name)
                    read_method = get_read_method(field.type, field.name)
                    lines.append(f'            {prop_name} = {read_method},')
                lines.append('        };')
        lines.append('    }')
    
    lines.append('}')
    return '\n'.join(lines)

def get_write_method(cs_type: str, prop_name: str, field_name: str = '') -> str:
    """Get the appropriate TLWriteBuffer.Write* method call"""
    type_map = {
        'int': f'writer.WriteInt32({prop_name});',
        'uint': f'writer.WriteUInt32({prop_name});',
        'long': f'writer.WriteInt64({prop_name});',
        'bool': f'writer.WriteBool({prop_name});',
        'string': f'writer.WriteString({prop_name});',
    }
    
    # Special handling for int256 byte arrays (32 bytes)
    if cs_type == 'byte[]':
        field_lower = field_name.lower()
        if 'hash' in field_lower or 'account' in field_lower or ('id' in field_lower and '_id' not in field_lower):
            return f'writer.WriteBytes({prop_name}, 32);'
        return f'writer.WriteBuffer({prop_name});'
    
    # Handle nullable types
    if cs_type.endswith('?'):
        base_type = cs_type[:-1]
        if base_type in type_map:
            return type_map[base_type].replace(f'({prop_name}', f'({prop_name}.Value')
    
    if cs_type in type_map:
        return type_map[cs_type]
    
    # Handle arrays
    if cs_type.endswith('[]') and cs_type != 'byte[]':
        return f'// TODO: Write array {prop_name}'
    
    # Handle custom types (they have WriteTo methods)
    return f'{prop_name}.WriteTo(writer);'

def get_read_method(cs_type: str, field_name: str = '') -> str:
    """Get the appropriate TLReadBuffer.Read* method call"""
    type_map = {
        'int': 'reader.ReadInt32()',
        'uint': 'reader.ReadUInt32()',
        'long': 'reader.ReadInt64()',
        'bool': 'reader.ReadBool()',
        'string': 'reader.ReadString()',
        'byte[]': 'reader.ReadBuffer()',
    }
    
    # Special handling for int256 and int128 (identified by field names)
    if cs_type == 'byte[]':
        field_lower = field_name.lower()
        if 'hash' in field_lower or 'account' in field_lower or 'id' in field_lower and '_id' not in field_lower:
            return 'reader.ReadInt256()'  # 32 bytes
        return 'reader.ReadBuffer()'
    
    # Handle nullable types
    if cs_type.endswith('?'):
        base_type = cs_type[:-1]
        if base_type in type_map:
            return type_map[base_type]
    
    if cs_type in type_map:
        return type_map[cs_type]
    
    # Handle arrays
    if cs_type.endswith('[]') and cs_type != 'byte[]':
        return f'Array.Empty<{cs_type[:-2]}>()'  # TODO: Proper array reading
    
    # Handle custom types
    return f'{cs_type}.ReadFrom(reader)'

def parse_tl_file(content: str) -> Tuple[List[TLType], List[TLType]]:
    """Parse entire TL file into types and functions"""
    lines = content.split('\n')
    types = []
    functions = []
    is_functions_section = False
    
    for line in lines:
        line = line.strip()
        if line == '---functions---':
            is_functions_section = True
            continue
        
        if line.startswith('//') or not line or line.startswith('---'):
            continue
        
        tl_type = parse_tl_line(line, is_function=is_functions_section)
        if tl_type:
            if is_functions_section:
                functions.append(tl_type)
            else:
                types.append(tl_type)
    
    return types, functions

def generate_csharp_code(types: List[TLType], functions: List[TLType]) -> str:
    """Generate complete C# schema file"""
    # Find union types (multiple types with same result_type)
    result_type_map = {}
    for t in types:
        if t.result_type:
            if t.result_type not in result_type_map:
                result_type_map[t.result_type] = []
            result_type_map[t.result_type].append(t)
    
    union_types = {rt: impls for rt, impls in result_type_map.items() if len(impls) > 1}
    
    lines = []
    lines.append('// Auto-generated from lite_api.tl')
    lines.append('// DO NOT EDIT MANUALLY')
    if union_types:
        lines.append(f'// Union types: {", ".join(union_types.keys())}')
    lines.append('')
    lines.append('using System;')
    lines.append('using TonSdk.Adnl.TL;')
    lines.append('')
    lines.append('namespace TonSdk.Adnl.LiteClient')
    lines.append('{')
    
    # Generate abstract base classes for union types
    if union_types:
        lines.append('    // ============================================================================')
        lines.append('    // Abstract base classes for union types')
        lines.append('    // ============================================================================')
        lines.append('')
        for result_type, implementations in union_types.items():
            abstract_class_name = to_pascal_case(result_type)
            lines.append(f'    /// <summary>')
            lines.append(f'    /// Base class for {result_type}')
            lines.append(f'    /// Implementations: {", ".join(to_pascal_case(t.name) for t in implementations)}')
            lines.append(f'    /// </summary>')
            lines.append(f'    public abstract class {abstract_class_name}')
            lines.append('    {')
            lines.append('        public abstract uint Constructor { get; }')
            lines.append('        public abstract void WriteTo(TLWriteBuffer writer);')
            lines.append('    }')
            lines.append('')
    
    # Generate basic types (as structs)
    basic_types = [t for t in types if t.name.startswith('tonNode.')]
    if basic_types:
        lines.append('    // ============================================================================')
        lines.append('    // Basic Types (tonNode.*)')
        lines.append('    // ============================================================================')
        lines.append('')
        for tl_type in basic_types:
            for line in generate_struct_or_class(tl_type, is_struct=True, union_types=union_types).split('\n'):
                lines.append('    ' + line if line else '')
            lines.append('')
    
    # Generate liteServer types (as classes)
    lite_types = [t for t in types if t.name.startswith('liteServer.') and not t.is_function]
    if lite_types:
        lines.append('    // ============================================================================')
        lines.append('    // Lite Server Types (liteServer.*)')
        lines.append('    // ============================================================================')
        lines.append('')
        for tl_type in lite_types:
            for line in generate_struct_or_class(tl_type, is_struct=False, union_types=union_types).split('\n'):
                lines.append('    ' + line if line else '')
            lines.append('')
    
    # Generate function constructors
    if functions:
        lines.append('    // ============================================================================')
        lines.append('    // Function Constructors')
        lines.append('    // ============================================================================')
        lines.append('')
        lines.append('    public static class Functions')
        lines.append('    {')
        for func in functions:
            const_name = to_pascal_case(func.name.replace('liteServer.', ''))
            lines.append(f'        public const uint {const_name} = 0x{func.constructor:08X};')
        lines.append('    }')
    
    lines.append('}')
    return '\n'.join(lines)

def main():
    print("Fetching lite_api.tl from TON repository...")
    with urllib.request.urlopen(LITE_API_URL) as response:
        content = response.read().decode('utf-8')
    
    print("Parsing TL schema...")
    types, functions = parse_tl_file(content)
    
    print(f"Found {len(types)} types and {len(functions)} functions")
    
    print("Generating C# code...")
    csharp_code = generate_csharp_code(types, functions)
    
    output_path = "../TonSdk.Adnl/src/LiteClient/Schema.Generated.cs"
    print(f"Writing to {output_path}...")
    with open(output_path, 'w') as f:
        f.write(csharp_code)
    
    print("✅ Done! Generated Schema.Generated.cs")
    print("\nFunction constructors:")
    for func in functions[:5]:
        print(f"  {func.name} = 0x{func.constructor:08X}")
    if len(functions) > 5:
        print(f"  ... and {len(functions) - 5} more")

if __name__ == '__main__':
    main()

