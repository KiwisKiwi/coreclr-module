﻿using System.Collections;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using AltV.Net.CApi;
using AltV.Net.Data;
using AltV.Net.Elements.Args;
using AltV.Net.Elements.Entities;
using AltV.Net.Exceptions;
using AltV.Net.Native;
using AltV.Net.Shared.Elements.Entities;
using AltV.Net.Shared.Utils;
using AltV.Net.Types;

namespace AltV.Net.Shared
{
    public abstract class SharedCore : ISharedCore, IDisposable
    {
        public IntPtr NativePointer { get; }

        public ILibrary Library { get; }

        public SharedCore(IntPtr nativePointer, ILibrary library)
        {         
            NativePointer = nativePointer;
            Library = library;
            MainThread = Thread.CurrentThread;
        }
        
        public abstract ISharedNativeResource Resource { get; }
        public abstract IReadOnlyEntityPool<ISharedPlayer> PlayerPool { get; }
        public abstract IReadOnlyEntityPool<ISharedVehicle> VehiclePool { get; }
        public abstract IReadOnlyBaseBaseObjectPool BaseBaseObjectPool { get; }

        private string? version;
        public string Version
        {
            get
            {
                unsafe
                {
                    if (version != null) return version;
                    var size = 0;
                    version = PtrToStringUtf8AndFree(
                        Library.Shared.Core_GetVersion(NativePointer, &size), size);

                    return version;
                }
            }
        }
        
        private string? branch;
        public string Branch
        {
            get
            {
                unsafe
                {
                    if (branch != null) return branch;
                    var size = 0;
                    branch = PtrToStringUtf8AndFree(
                        Library.Shared.Core_GetBranch(NativePointer, &size), size);

                    return branch;
                }
            }
        }
        
        private bool? isDebug;
        public bool IsDebug
        {
            get
            {
                unsafe
                {
                    if (isDebug.HasValue) return isDebug.Value;
                    isDebug = Library.Shared.Core_IsDebug(NativePointer) == 1;
                    return isDebug.Value;
                }
            }
        }

        public uint Hash(string stringToHash)
        {
            if (string.IsNullOrEmpty(stringToHash)) return 0;

            var characters = Encoding.UTF8.GetBytes(stringToHash.ToLower());

            uint hash = 0;

            foreach (var c in characters)
            {
                hash += c;
                hash += hash << 10;
                hash ^= hash >> 6;
            }

            hash += hash << 3;
            hash ^= hash >> 11;
            hash += hash << 15;

            return hash;
        }
        
        public void LogInfo(IntPtr messagePtr)
        {
            unsafe
            {
                Library.Shared.Core_LogInfo(NativePointer, messagePtr);
            }
        }

        public void LogDebug(IntPtr messagePtr)
        {
            unsafe
            {
                Library.Shared.Core_LogDebug(NativePointer, messagePtr);
            }
        }

        public void LogWarning(IntPtr messagePtr)
        {
            unsafe
            {
                Library.Shared.Core_LogWarning(NativePointer, messagePtr);
            }
        }

        public void LogError(IntPtr messagePtr)
        {
            unsafe
            {
                Library.Shared.Core_LogError(NativePointer, messagePtr);
            }
        }

        public void LogColored(IntPtr messagePtr)
        {
            unsafe
            {
                Library.Shared.Core_LogColored(NativePointer, messagePtr);
            }
        }

        public void LogInfo(string message)
        {
            unsafe
            {
                var messagePtr = MemoryUtils.StringToHGlobalUtf8(message);
                Library.Shared.Core_LogInfo(NativePointer, messagePtr);
                Marshal.FreeHGlobal(messagePtr);
            }
        }

        public void LogDebug(string message)
        {
            unsafe
            {
                var messagePtr = MemoryUtils.StringToHGlobalUtf8(message);
                Library.Shared.Core_LogDebug(NativePointer, messagePtr);
                Marshal.FreeHGlobal(messagePtr);
            }
        }

        public void LogWarning(string message)
        {
            unsafe
            {
                var messagePtr = MemoryUtils.StringToHGlobalUtf8(message);
                Library.Shared.Core_LogWarning(NativePointer, messagePtr);
                Marshal.FreeHGlobal(messagePtr);
            }
        }

        public void LogError(string message)
        {
            unsafe
            {
                var messagePtr = MemoryUtils.StringToHGlobalUtf8(message);
                Library.Shared.Core_LogError(NativePointer, messagePtr);
                Marshal.FreeHGlobal(messagePtr);
            }
        }

        public void LogColored(string message)
        {
            unsafe
            {
                var messagePtr = MemoryUtils.StringToHGlobalUtf8(message);
                Library.Shared.Core_LogColored(NativePointer, messagePtr);
                Marshal.FreeHGlobal(messagePtr);
            }
        }
        
        public string PtrToStringUtf8AndFree(nint str, int size)
        {
            if (str == IntPtr.Zero) return string.Empty;
            unsafe
            {
                var stringResult = Marshal.PtrToStringUTF8(str, size);
                Library.Shared.FreeString(str);
                return stringResult;
            }
        }
        
        private readonly IDictionary<int, IDictionary<IRefCountable, ulong>> threadRefCount =
            new Dictionary<int, IDictionary<IRefCountable, ulong>>();

        [Conditional("DEBUG")]
        public void CountUpRefForCurrentThread(IRefCountable baseObject)
        {
            if (baseObject == null) return;
            var currThread = Thread.CurrentThread.ManagedThreadId;
            lock (threadRefCount)
            {
                if (!threadRefCount.TryGetValue(currThread, out var baseObjectRefCount))
                {
                    baseObjectRefCount = new Dictionary<IRefCountable, ulong>();
                    threadRefCount[currThread] = baseObjectRefCount;
                }

                if (!baseObjectRefCount.TryGetValue(baseObject, out var count))
                {
                    count = 0;
                }

                baseObjectRefCount[baseObject] = count + 1;
            }
        }

        [Conditional("DEBUG")]
        public void CountDownRefForCurrentThread(IRefCountable baseObject)
        {
            if (baseObject == null) return;
            var currThread = Thread.CurrentThread.ManagedThreadId;
            lock (threadRefCount)
            {
                if (!threadRefCount.TryGetValue(currThread, out var baseObjectRefCount))
                {
                    return;
                }

                if (!baseObjectRefCount.TryGetValue(baseObject, out var count))
                {
                    return;
                }

                if (count == 1)
                {
                    baseObjectRefCount.Remove(baseObject);
                    return;
                }

                baseObjectRefCount[baseObject] = count - 1;
            }
        }
        
        public bool HasRefForCurrentThread(IRefCountable baseObject)
        {
            var currThread = Thread.CurrentThread.ManagedThreadId;
            lock (threadRefCount)
            {
                if (!threadRefCount.TryGetValue(currThread, out var baseObjectRefCount))
                {
                    return false;
                }

                if (!baseObjectRefCount.TryGetValue(baseObject, out var count))
                {
                    return false;
                }

                return count > 0;
            }
        }
        
        public virtual void Dispose()
        {
            
        }
        

        protected readonly Thread MainThread;

        public virtual bool IsMainThread()
        {
            return Thread.CurrentThread == MainThread;
        }

        [Conditional("DEBUG")]
        public void CheckIfCallIsValid([CallerMemberName] string callerName = "")
        {
            if (IsMainThread()) return;
            throw new IllegalThreadException(this, callerName);
        }
        
        public ISharedEntity GetEntityById(ushort id)
        {
            unsafe
            {
                CheckIfCallIsValid();
                var type = (byte) BaseObjectType.Undefined;
                var entityPointer = Library.Shared.Core_GetEntityById(NativePointer, id, &type);
                if (entityPointer == IntPtr.Zero) return null;
                switch (type)
                {
                    case (byte) BaseObjectType.Player:
                        return PlayerPool.Get(entityPointer);
                    case (byte) BaseObjectType.Vehicle:
                        return VehiclePool.Get(entityPointer);
                    default:
                        return null;
                }
            }
        }

        #region MValues
        public void CreateMValueNil(out MValueConst mValue)
        {
            unsafe
            {
                mValue = new MValueConst(this, MValueConst.Type.Nil, Library.Shared.Core_CreateMValueNil(NativePointer));
            }
        }

        public void CreateMValueBool(out MValueConst mValue, bool value)
        {
            unsafe
            {
                mValue = new MValueConst(this, MValueConst.Type.Bool,
                    Library.Shared.Core_CreateMValueBool(NativePointer, value ? (byte) 1 : (byte) 0));
            }
        }

        public void CreateMValueInt(out MValueConst mValue, long value)
        {
            unsafe
            {
                mValue = new MValueConst(this, MValueConst.Type.Int, Library.Shared.Core_CreateMValueInt(NativePointer, value));
            }
        }

        public void CreateMValueUInt(out MValueConst mValue, ulong value)
        {
            unsafe
            {
                mValue = new MValueConst(this, MValueConst.Type.Uint,
                    Library.Shared.Core_CreateMValueUInt(NativePointer, value));
            }
        }

        public void CreateMValueDouble(out MValueConst mValue, double value)
        {
            unsafe
            {
                mValue = new MValueConst(this, MValueConst.Type.Double,
                    Library.Shared.Core_CreateMValueDouble(NativePointer, value));
            }
        }

        public void CreateMValueString(out MValueConst mValue, string value)
        {
            unsafe
            {
                var valuePtr = MemoryUtils.StringToHGlobalUtf8(value);
                mValue = new MValueConst(this, MValueConst.Type.String,
                    Library.Shared.Core_CreateMValueString(NativePointer, valuePtr));
                Marshal.FreeHGlobal(valuePtr);
            }
        }

        public void CreateMValueList(out MValueConst mValue, MValueConst[] val, ulong size)
        {
            unsafe
            {
                var pointers = new IntPtr[size];
                for (ulong i = 0; i < size; i++)
                {
                    pointers[i] = val[i].nativePointer;
                }

                mValue = new MValueConst(this, MValueConst.Type.List,
                    Library.Shared.Core_CreateMValueList(NativePointer, pointers, size));
            }
        }

        public void CreateMValueDict(out MValueConst mValue, string[] keys, MValueConst[] val, ulong size)
        {
            unsafe
            {
                var pointers = new IntPtr[size];
                for (ulong i = 0; i < size; i++)
                {
                    pointers[i] = val[i].nativePointer;
                }
                
                var keyPointers = new IntPtr[size];
                for (ulong i = 0; i < size; i++)
                {
                    keyPointers[i] = MemoryUtils.StringToHGlobalUtf8(keys[i]);
                }

                mValue = new MValueConst(this, MValueConst.Type.Dict,
                    Library.Shared.Core_CreateMValueDict(NativePointer, keyPointers, pointers, size));
                for (ulong i = 0; i < size; i++)
                {
                    Marshal.FreeHGlobal(keyPointers[i]);  
                }
            }
        }

        public void CreateMValueBaseObject(out MValueConst mValue, ISharedBaseObject value)
        {
            unsafe
            {
                mValue = new MValueConst(this, MValueConst.Type.BaseObject,
                    Library.Shared.Core_CreateMValueBaseObject(NativePointer, value.BaseObjectNativePointer));
            }
        }

        public void CreateMValueFunction(out MValueConst mValue, IntPtr value)
        {
            unsafe
            {
                mValue = new MValueConst(this, MValueConst.Type.Function,
                    Library.Shared.Core_CreateMValueFunction(NativePointer, value));
            }
        }

        public void CreateMValueVector3(out MValueConst mValue, Position value)
        {
            unsafe
            {
                mValue = new MValueConst(this, MValueConst.Type.Vector3,
                    Library.Shared.Core_CreateMValueVector3(NativePointer, value));
            }
        }
        
        public void CreateMValueVector2(out MValueConst mValue, Vector2 value)
        {
            unsafe
            {
                mValue = new MValueConst(this, MValueConst.Type.Vector2,
                    Library.Shared.Core_CreateMValueVector2(NativePointer, value));
            }
        }

        public void CreateMValueRgba(out MValueConst mValue, Rgba value)
        {
            unsafe
            {
                mValue = new MValueConst(this, MValueConst.Type.Rgba,
                    Library.Shared.Core_CreateMValueRgba(NativePointer, value));
            }
        }

        public void CreateMValueByteArray(out MValueConst mValue, byte[] value)
        {
            unsafe
            {
                var size = value.Length;
                var dataPtr = Marshal.AllocHGlobal(size);
                Marshal.Copy(value, 0, dataPtr, size);
                mValue = new MValueConst(this, MValueConst.Type.ByteArray,
                    Library.Shared.Core_CreateMValueByteArray(NativePointer, (ulong) size, dataPtr));
                Marshal.FreeHGlobal(dataPtr);
            }
        }

        public void CreateMValue(out MValueConst mValue, object obj)
        {
            if (obj == null)
            {
                mValue = MValueConst.Nil;
                return;
            }

            int i;

            string[] dictKeys;
            MValueConst[] dictValues;
            MValueWriter2 writer;

            switch (obj)
            {
                case ISharedBaseObject baseObject:
                    CreateMValueBaseObject(out mValue, baseObject);
                    return;
                case bool value:
                    CreateMValueBool(out mValue, value);
                    return;
                case int value:
                    CreateMValueInt(out mValue, value);
                    return;
                case uint value:
                    CreateMValueUInt(out mValue, value);
                    return;
                case long value:
                    CreateMValueInt(out mValue, value);
                    return;
                case ulong value:
                    CreateMValueUInt(out mValue, value);
                    return;
                case double value:
                    CreateMValueDouble(out mValue, value);
                    return;
                case float value:
                    CreateMValueDouble(out mValue, value);
                    return;
                case string value:
                    CreateMValueString(out mValue, value);
                    return;
                case MValueConst value:
                    mValue = value;
                    return;
                case MValueConst[] value:
                    CreateMValueList(out mValue, value, (ulong) value.Length);
                    return;
                case Invoker value:
                    CreateMValueFunction(out mValue, value.NativePointer);
                    return;
                case MValueFunctionCallback value:
                    CreateMValueFunction(out mValue, Resource.CSharpResourceImpl.CreateInvoker(value));
                    return;
                case Function function:
                    CreateMValueFunction(out mValue,
                        Resource.CSharpResourceImpl.CreateInvoker(function.Call));
                    return;
                case byte[] byteArray:
                    CreateMValueByteArray(out mValue, byteArray);
                    return;
                case IDictionary dictionary:
                    dictKeys = new string[dictionary.Count];
                    dictValues = new MValueConst[dictionary.Count];
                    i = 0;
                    foreach (var key in dictionary.Keys)
                    {
                        if (key is string stringKey)
                        {
                            dictKeys[i++] = stringKey;
                        }
                        else
                        {
                            mValue = MValueConst.Nil;
                            return;
                        }
                    }

                    i = 0;
                    foreach (var value in dictionary.Values)
                    {
                        CreateMValue(out var elementMValue, value);
                        dictValues[i++] = elementMValue;
                    }

                    CreateMValueDict(out mValue, dictKeys, dictValues, (ulong) dictionary.Count);
                    for (int j = 0, dictLength = dictionary.Count; j < dictLength; j++)
                    {
                        dictValues[j].Dispose();
                    }

                    return;
                case ICollection collection:
                    var length = (ulong) collection.Count;
                    var listValues = new MValueConst[length];
                    i = 0;
                    foreach (var value in collection)
                    {
                        CreateMValue(out var elementMValue, value);
                        listValues[i++] = elementMValue;
                    }

                    CreateMValueList(out mValue, listValues, length);
                    for (ulong j = 0; j < length; j++)
                    {
                        listValues[j].Dispose();
                    }

                    return;
                case IDictionary<string, object> dictionary:
                    dictKeys = new string[dictionary.Count];
                    dictValues = new MValueConst[dictionary.Count];
                    i = 0;
                    foreach (var key in dictionary.Keys)
                    {
                        dictKeys[i++] = key;
                    }

                    i = 0;
                    foreach (var value in dictionary.Values)
                    {
                        CreateMValue(out var elementMValue, value);
                        dictValues[i++] = elementMValue;
                    }

                    CreateMValueDict(out mValue, dictKeys, dictValues, (ulong) dictionary.Count);
                    for (int j = 0, dictLength = dictionary.Count; j < dictLength; j++)
                    {
                        dictValues[j].Dispose();
                    }

                    return;
                case IWritable writable:
                    writer = new MValueWriter2(this);
                    writable.OnWrite(writer);
                    writer.ToMValue(out mValue);
                    return;
                case IMValueConvertible convertible:
                    writer = new MValueWriter2(this);
                    convertible.GetAdapter().ToMValue(obj, writer);
                    writer.ToMValue(out mValue);
                    return;
                case Position position:
                    CreateMValueVector3(out mValue, position);
                    return;
                case Rotation rotation:
                    CreateMValueVector3(out mValue, rotation);
                    return;
                case Rgba rgba:
                    CreateMValueRgba(out mValue, rgba);
                    return;
                case short value:
                    CreateMValueInt(out mValue, value);
                    return;
                case ushort value:
                    CreateMValueUInt(out mValue, value);
                    return;
                case Vector3 position:
                    CreateMValueVector3(out mValue, position);
                    return;
                case Vector2 value:
                    CreateMValueVector2(out mValue, value);
                    return;
                default:
                    LogInfo("can't convert type:" + obj.GetType());
                    mValue = MValueConst.Nil;
                    return;
            }
        }

        public void CreateMValues(MValueConst[] mValues, object[] objects)
        {
            for (int i = 0, length = objects.Length; i < length; i++)
            {
                CreateMValue(out var mValue, objects[i]);
                mValues[i] = mValue;
            }
        }
        #endregion
        
        #region MValueAdapters
        
        private readonly Dictionary<Type, IMValueBaseAdapter> adapters =
            new Dictionary<Type, IMValueBaseAdapter>();

        public void RegisterMValueAdapter<T>(IMValueAdapter<T> adapter)
        {
            adapters[typeof(T)] = adapter;
        }

        public bool ToMValue(object obj, Type type, out MValueConst mValue)
        {
            if (adapters.TryGetValue(type, out var adapter))
            {
                var writer = new MValueWriter2(this);
                adapter.ToMValue(obj, writer);
                writer.ToMValue(out mValue);
                return true;
            }

            mValue = default;
            return false;
        }

        public bool FromMValue(in MValueConst mValue, Type type, out object obj)
        {
            switch (mValue.type)
            {
                case MValueConst.Type.List when adapters.TryGetValue(type, out var adapter):
                {
                    using (var reader = new MValueReader2(this, in mValue))
                    {
                        obj = adapter.FromMValue(reader);
                    }

                    return true;
                }
                case MValueConst.Type.Dict when adapters.TryGetValue(type, out var adapter):
                    using (var reader = new MValueReader2(this, in mValue))
                    {
                        obj = adapter.FromMValue(reader);
                    }

                    return true;
                default:
                    obj = null;
                    return false;
            }
        }

        public bool MValueFromObject(object obj, Type type, out object result)
        {
            if (adapters.TryGetValue(type, out var adapter))
            {
                result = adapter.FromMValue(new MValueObjectReader(obj));
                return true;
            }

            result = null;
            return false;
        }

        public bool IsMValueConvertible(Type type)
        {
            return adapters.ContainsKey(type);
        }
        #endregion
        
        #region Metadata
        public void GetMetaData(string key, out MValueConst value)
        {
            unsafe
            {
                CheckIfCallIsValid();
                var stringPtr = AltNative.StringUtils.StringToHGlobalUtf8(key);
                value = new MValueConst(this, Library.Shared.Core_GetMetaData(NativePointer, stringPtr));
                Marshal.FreeHGlobal(stringPtr);
            }
        }

        public void SetMetaData(string key, object value)
        {
            unsafe
            {
                CheckIfCallIsValid();
                CreateMValue(out var mValue, value);
                var stringPtr = AltNative.StringUtils.StringToHGlobalUtf8(key);
                Library.Shared.Core_SetMetaData(NativePointer, stringPtr, mValue.nativePointer);
                Marshal.FreeHGlobal(stringPtr);
                mValue.Dispose();
            }
        }

        public bool HasMetaData(string key)
        {
            unsafe
            {
                CheckIfCallIsValid();
                var stringPtr = AltNative.StringUtils.StringToHGlobalUtf8(key);
                var result = Library.Shared.Core_HasMetaData(NativePointer, stringPtr);
                Marshal.FreeHGlobal(stringPtr);
                return result == 1;
            }
        }

        public void DeleteMetaData(string key)
        {
            unsafe
            {
                CheckIfCallIsValid();
                var stringPtr = AltNative.StringUtils.StringToHGlobalUtf8(key);
                Library.Shared.Core_DeleteMetaData(NativePointer, stringPtr);
                Marshal.FreeHGlobal(stringPtr);
            }
        }
        
        public void GetSyncedMetaData(string key, out MValueConst value)
        {
            unsafe
            {
                CheckIfCallIsValid();
                var stringPtr = AltNative.StringUtils.StringToHGlobalUtf8(key);
                value = new MValueConst(this, Library.Shared.Core_GetSyncedMetaData(NativePointer, stringPtr));
                Marshal.FreeHGlobal(stringPtr);
            }
        }
        
        public bool HasSyncedMetaData(string key)
        {
            unsafe
            {
                CheckIfCallIsValid();
                var stringPtr = AltNative.StringUtils.StringToHGlobalUtf8(key);
                var result = Library.Shared.Core_HasSyncedMetaData(NativePointer, stringPtr);
                Marshal.FreeHGlobal(stringPtr);
                return result == 1;
            }
        }
        #endregion
    }
}