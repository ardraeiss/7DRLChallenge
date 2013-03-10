using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using libtcod;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace roguelike
{

    static class Program
    {

        private static SaveState load()
        {
            if (File.Exists(Globals.SAVE))
            {
                try
                {
                    IFormatter deserializer = new BinaryFormatter();
                    Stream loadStream = new FileStream(Globals.SAVE, FileMode.Open, FileAccess.Read, FileShare.Read);
                    SaveState saveState = (SaveState)deserializer.Deserialize(loadStream);
                    loadStream.Close();
                    return saveState;
                }
                catch
                {
                    System.Diagnostics.Debug.WriteLine("Error loading");
                    return null;
                }
            }

            return null;
        }

        static void Main()
        {
            SaveState saved = load();
            Engine engine;
            State gameState;

            if (saved != null)
            {
                gameState = new State(saved);
                engine = new Engine(gameState, true, new State());
            }
            else
            {
                gameState = new State();
                engine = new Engine(gameState);
            }

            while (!TCODConsole.isWindowClosed())
            {

                engine.update();
                engine.render();
                if (engine.gStatus == Engine.Status.LOSE || engine.gStatus == Engine.Status.WIN)
                {
                    break;
                }
                TCODConsole.flush();
            }

            engine.saveClose();

        }
    }
}
