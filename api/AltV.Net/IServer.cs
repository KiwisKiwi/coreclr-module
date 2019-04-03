using System;
using AltV.Net.Data;
using AltV.Net.Elements.Entities;
using AltV.Net.Elements.Args;

namespace AltV.Net
{
    public interface IServer
    {
        void LogInfo(string message);

        /// <summary>
        /// Do NOT use unless you know what you are doing
        /// </summary>
        void LogDebug(string message);

        void LogWarning(string message);

        void LogError(string message);

        void LogColored(string message);
        
        void LogInfo(IntPtr message);

        /// <summary>
        /// Do NOT use unless you know what you are doing
        /// </summary>
        void LogDebug(IntPtr message);

        void LogWarning(IntPtr message);

        void LogError(IntPtr message);

        void LogColored(IntPtr message);

        uint Hash(string hash);

        void TriggerServerEvent(IntPtr eventNamePtr, params MValue[] args);

        void TriggerServerEvent(IntPtr eventNamePtr, params object[] args);

        void TriggerServerEvent(IntPtr eventNamePtr, ref MValue args);

        void TriggerServerEvent(string eventName, params MValue[] args);

        void TriggerServerEvent(string eventName, params object[] args);

        void TriggerServerEvent(string eventName, ref MValue args);

        void TriggerClientEvent(IPlayer player, IntPtr eventNamePtr, params MValue[] args);

        void TriggerClientEvent(IPlayer player, IntPtr eventNamePtr, params object[] args);

        void TriggerClientEvent(IPlayer player, IntPtr eventNamePtr, ref MValue args);

        void TriggerClientEvent(IPlayer player, string eventName, params MValue[] args);

        void TriggerClientEvent(IPlayer player, string eventName, params object[] args);

        void TriggerClientEvent(IPlayer player, string eventName, ref MValue args);

        IVehicle CreateVehicle(uint model, Position pos, float heading);

        ICheckpoint CreateCheckpoint(IPlayer player, byte type, Position pos, float radius, float height, Rgba color);

        IBlip CreateBlip(IPlayer player, byte type, Position pos);

        IBlip CreateBlip(IPlayer player, byte type, IEntity entityAttach);

        IVoiceChannel CreateVoiceChannel(bool spatial, float maxDistance);

        void RemoveEntity(IEntity entity);

        void RemoveBlip(IBlip blip);

        void RemoveCheckpoint(ICheckpoint checkpoint);

        void RemoveVehicle(IVehicle vehicle);

        void RemoveVoiceChannel(IVoiceChannel channel);

        ServerNativeResource GetResource(string name);
    }
}