using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using LightningDB.Native;
using static LightningDB.Native.Lmdb;

namespace LightningDB;

/// <summary>
/// Represents the configuration for a database in the LightningDB library.
/// Allows setting custom flags and configuring comparer logic for database operations.
/// </summary>
public class DatabaseConfiguration
{
    private IComparer<MDBValue>? _comparer;
    private IComparer<MDBValue>? _duplicatesComparer;
    private nint _nativeComparer;
    private nint _nativeDuplicatesComparer;

    public DatabaseConfiguration()
    {
        Flags = DatabaseOpenFlags.None;
    }

    /// <summary>
    /// Gets or sets the configuration flags used when opening a database.
    /// </summary>
    /// <remarks>
    /// The <see cref="Flags"/> property specifies the behavior of the database based on the combination of
    /// values from the <see cref="DatabaseOpenFlags"/> enumeration. These flags determine how the database
    /// should be opened and interacted with, such as creating new databases, sorting duplicates, or using
    /// integer keys. The default value is <see cref="DatabaseOpenFlags.None"/>.
    /// </remarks>
    public DatabaseOpenFlags Flags { get; set; }


    internal IDisposable ConfigureDatabase(LightningTransaction tx, LightningDatabase db)
    {
        var pinnedComparer = new ComparerKeepAlive();
        if (_nativeComparer != 0)
        {
            mdb_set_compare(tx._handle, db._handle, _nativeComparer);
        }
        else if (_comparer != null)
        {
            CompareFunction compare = Compare;
            pinnedComparer.AddComparer(compare);
            mdb_set_compare(tx._handle, db._handle, compare);
        }

        if (_nativeDuplicatesComparer != 0)
        {
            mdb_set_dupsort(tx._handle, db._handle, _nativeDuplicatesComparer);
            return pinnedComparer;
        }

        if (_duplicatesComparer == null) return pinnedComparer;
        CompareFunction dupCompare = IsDuplicate;
        pinnedComparer.AddComparer(dupCompare);
        mdb_set_dupsort(tx._handle, db._handle, dupCompare);
        return pinnedComparer;
    }

    private int Compare(ref MDBValue left, ref MDBValue right)
    {
        return _comparer!.Compare(left, right);
    }

    private int IsDuplicate(ref MDBValue left, ref MDBValue right)
    {
        return _duplicatesComparer!.Compare(left, right);
    }

    /// <summary>
    /// Sets a custom comparer for database operations using the specified comparer.
    /// </summary>
    /// <param name="comparer">
    /// The comparer implementation to use for comparing MDBValue objects.
    /// </param>
    public void CompareWith(IComparer<MDBValue> comparer)
    {
        _comparer = comparer;
    }

    /// <summary>
    /// Sets a custom comparer for detecting duplicate records in the database.
    /// </summary>
    /// <param name="comparer">
    /// The comparer implementation to use for identifying duplicates between MDBValue objects.
    /// </param>
    public void FindDuplicatesWith(IComparer<MDBValue> comparer)
    {
        _duplicatesComparer = comparer;
    }

    /// <summary>
    /// Sets a custom comparer given as a native function pointer, typically a static method marked
    /// <c>[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]</c>. Unlike <see cref="CompareWith(IComparer{MDBValue})"/>
    /// this needs no marshaled delegate, which browser-wasm cannot hand to native code.
    /// </summary>
    /// <param name="comparer">A function comparing two keys, returning less than, equal to or greater than zero.</param>
    public unsafe void CompareWith(delegate* unmanaged[Cdecl]<MDBValue*, MDBValue*, int> comparer)
    {
        _nativeComparer = (nint)comparer;
    }

    /// <summary>
    /// Sets a custom duplicate comparer given as a native function pointer, like the native-pointer overload of
    /// <c>CompareWith</c>.
    /// </summary>
    /// <param name="comparer">A function comparing two values, returning less than, equal to or greater than zero.</param>
    public unsafe void FindDuplicatesWith(delegate* unmanaged[Cdecl]<MDBValue*, MDBValue*, int> comparer)
    {
        _nativeDuplicatesComparer = (nint)comparer;
    }

    private class ComparerKeepAlive : IDisposable
    {
        private readonly List<GCHandle> _comparisons = new();

        public void AddComparer(CompareFunction compare)
        {
            var handle = GCHandle.Alloc(compare);
            _comparisons.Add(handle);
        }

        public void Dispose()
        {
            for (var i = 0; i < _comparisons.Count; ++i)
            {
                _comparisons[i].Free();
            }
        }
    }
}