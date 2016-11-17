﻿using NintendoSpy.Readers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NintendoSpy
{
    public class InputSource
    {
        static public readonly InputSource NES = new InputSource ("nes", "NES", true, port => new SerialControllerReader (port, SuperNESandNES.ReadFromPacket_NES));
        static public readonly InputSource SNES = new InputSource ("snes", "Super NES", true, port => new SerialControllerReader (port, SuperNESandNES.ReadFromPacket_SNES));
        static public readonly InputSource N64 = new InputSource ("n64", "Nintendo 64", true, port => new SerialControllerReader (port, Nintendo64.ReadFromPacket));
        static public readonly InputSource GAMECUBE = new InputSource ("gamecube", "GameCube", true, port => new SerialControllerReader (port, GameCube.ReadFromPacket));
        static public readonly InputSource WIIU_PRO = new InputSource("wiiupro", "WiiU Pro Controller", false, _ => new XInputReader());
        static public readonly InputSource PC360 = new InputSource ("pc360", "PC 360", false, _ => new XInputReader ());
        static public readonly InputSource PAD = new InputSource ("generic", "Generic PC Gamepad", false, _ => new GamepadReader ());

        static public readonly IReadOnlyList <InputSource> ALL = new List <InputSource> {
            NES, SNES, N64, GAMECUBE, WIIU_PRO, PC360, PAD
        };

        static public readonly InputSource DEFAULT = NES;

        public string TypeTag { get; private set; }
        public string Name { get; private set; }
        public bool RequiresComPort { get; private set; }

        public Func <string, IControllerReader> BuildReader { get; private set; }

        InputSource (string typeTag, string name, bool requiresComPort, Func <string, IControllerReader> buildReader) {
            TypeTag = typeTag;
            Name = name;
            RequiresComPort = requiresComPort;
            BuildReader = buildReader;
        }
    }
}
