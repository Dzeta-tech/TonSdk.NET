using System;
using System.Numerics;
using TonSdk.Core.Addresses;
using TonSdk.Core.boc;
using TonSdk.Core.boc.bits;
using TonSdk.Core.boc.Cells;
using TonSdk.Core.Economics;
using CellSlice = TonSdk.Core.boc.Cells.CellSlice;

namespace TonSdk.Core.Blocks;

/// <summary>
///     Account status according to TL-B schema
/// </summary>
public enum AccountStatus
{
    Uninitialized = 0,
    Frozen = 1,
    Active = 2,
    Nonexist = 3
}

/// <summary>
///     Parsed account state from blockchain
/// </summary>
public class Account
{
    public Address Address { get; set; }
    public AccountStorage Storage { get; set; }

    public static Account Load(CellSlice slice)
    {
        // account_none$0 = Account;
        // account$1 addr:MsgAddressInt storage_stat:StorageInfo storage:AccountStorage = Account;
        
        if (!slice.LoadBit())
        {
            // account_none - doesn't exist
            throw new Exception("Account does not exist");
        }

        // Load address (from old SDK: slice.LoadAddress())
        Address? addr = slice.LoadAddress();
        if (addr == null)
        {
            throw new Exception("Invalid account address");
        }
        
        // Load storage_stat (StorageInfo)
        // From old SDK lines 154-161:
        // storage_used$_ cells:(VarUInteger 7) bits:(VarUInteger 7) public_cells:(VarUInteger 7) = StorageUsed;
        slice.LoadVarUInt(7); // cells
        slice.LoadVarUInt(7); // bits
        slice.LoadVarUInt(7); // public_cells
        
        // last_paid:uint32
        slice.LoadUInt(32);
        
        // due_payment:(Maybe Grams)
        if (slice.LoadBit())
        {
            slice.LoadCoins(); // due_payment
        }
        
        // Load storage (AccountStorage)
        AccountStorage storage = AccountStorage.Load(slice);
        
        return new Account
        {
            Address = addr.Value,
            Storage = storage
        };
    }
}

/// <summary>
///     Account storage containing balance and state
/// </summary>
public class AccountStorage
{
    public long LastTransLt { get; set; }
    public Coins Balance { get; set; }
    public AccountState State { get; set; }

    public static AccountStorage Load(CellSlice slice)
    {
        // account_storage$_ last_trans_lt:uint64 balance:CurrencyCollection state:AccountState = AccountStorage;
        // From old SDK lines 163-167:
        
        long lastTransLt = (long)slice.LoadUInt(64);
        Coins balance = slice.LoadCoins();
        
        // Load extra currencies dictionary (usually empty)
        // From old SDK line 176:
        var hmOptions = new HashmapOptions<int, int>()
        {
            KeySize = 32,
            Serializers = null,
            Deserializers = null
        };
        slice.LoadDict(hmOptions);
        
        AccountState state = AccountState.Load(slice);
        
        return new AccountStorage
        {
            LastTransLt = lastTransLt,
            Balance = balance,
            State = state
        };
    }
}

/// <summary>
///     Account state - can be uninitialized, frozen, or active
/// </summary>
public class AccountState
{
    public AccountStatus Status { get; set; }
    public Cell Code { get; set; }
    public Cell Data { get; set; }
    
    public static AccountState Load(CellSlice slice)
    {
        // account_uninit$00 = AccountState;
        // account_frozen$01 state_hash:bits256 = AccountState;
        // account_active$1 _:StateInit = AccountState;
        // From old SDK lines 178-204:
        
        if (slice.LoadBit()) // active
        {
            // StateInit structure: split_depth:(Maybe (## 5)) special:(Maybe TickTock) code:(Maybe ^Cell) data:(Maybe ^Cell) library:(Maybe ^Cell)
            
            // split_depth:(Maybe (## 5))
            if (slice.LoadBit())
                slice.LoadUInt(5);
            
            // special:(Maybe TickTock)
            if (slice.LoadBit())
            {
                slice.LoadBit(); // tick
                slice.LoadBit(); // tock
            }

            Cell code = null;
            if (slice.LoadBit())
                code = slice.LoadRef();
            
            Cell data = null;
            if (slice.LoadBit())
                data = slice.LoadRef();
            
            if (slice.LoadBit())
                slice.LoadRef(); // library

            return new AccountState
            {
                Status = AccountStatus.Active,
                Code = code,
                Data = data
            };
        }
        else if (slice.LoadBit()) // frozen
        {
            slice.LoadBits(256); // state_hash
            return new AccountState
            {
                Status = AccountStatus.Frozen,
                Code = null,
                Data = null
            };
        }
        else // uninit
        {
            return new AccountState
            {
                Status = AccountStatus.Uninitialized,
                Code = null,
                Data = null
            };
        }
    }
}

