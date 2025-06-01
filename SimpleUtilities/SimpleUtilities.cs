using LabApi.Loader.Features.Plugins;
using HarmonyLib;
using System;
using LabApi.Features;
using LabApi.Loader.Features.Plugins.Enums;
using LabApi.Events.CustomHandlers;


namespace SimpleUtilities
{
    public class SimpleUtilities : Plugin<Config>
    {
        public override string Name { get; } = "SimpleUtilities";
        public override string Author { get; } = "omgiamhungarian, KiwiSoupfx";
        public override string Description { get; } = "Provides simple features for your server.";
        public override Version Version { get; } = new Version(1, 4, 1);
        public override Version RequiredApiVersion { get; } = new Version(LabApiProperties.CompiledVersion);
        public EventHandlers Events { get; private set; } = new EventHandlers();
        public static SimpleUtilities Singleton { get; set; } = null!;

        public Harmony Harmony { get; private set; }

        public override LoadPriority Priority { get; } = LoadPriority.Highest; 

        public override void Enable()
        {
            Singleton = this;
            CustomHandlersManager.RegisterEventsHandler(Events);
            Harmony = new Harmony("com.kiwisoupfx.simpleutilities"); //Changing it for futureproofing
        }
        public override void Disable()
        {
            Singleton = null!;
            CustomHandlersManager.UnregisterEventsHandler(Events);
            Harmony = null;       
        }
    }
}