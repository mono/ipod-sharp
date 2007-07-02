using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace IPod
{
    internal class SysInfo
    {
        StreamReader textReader;
        string boardHWName, serialNumber, modelString;
        DeviceModel deviceModel;
        DeviceGeneration deviceGeneration;
        List<string> otherInfo=new List<string>();

        public string BoardHWName { get { return boardHWName; } }
        public string SerialNumber { get { return serialNumber; } }
        public string ModelString { get { return modelString.Substring(1); } }
        public DeviceModel DeviceModel { get { return deviceModel; } }
        public DeviceGeneration DeviceGeneration { get { return deviceGeneration; } }
        public string[] OtherInfo { get { return otherInfo.ToArray(); } }

        /// <exception cref="FileNotFoundException">SysInfo file not found</exception>
        public SysInfo(IDevice iPod)
        {
            string SysInfoPath = iPod.ControlPath + "Device\\SysInfo";

            if (!File.Exists(SysInfoPath))
                throw new FileNotFoundException();

            textReader = new StreamReader(SysInfoPath);

            ParseFile();
        }

        private void ParseFile()
        {
            while (!textReader.EndOfStream)
            {
                string textLine = textReader.ReadLine();
                string[] lineParts = textLine.Split(':');

                if (lineParts.Length > 1)
                {
                    switch (lineParts[0])
                    {
                        case "ModelNumStr":
                            modelString = lineParts[1].Substring(1);
                            ParseModelNumber(modelString, out deviceModel, out deviceGeneration);
                            break;
                        case "pszSerialNumber":
                            serialNumber = lineParts[1].Substring(1);
                            break;
                        default:
                            otherInfo.Add(textLine);
                            break;
                    }
                }
            }
        }

        private void ParseModelNumber(string modelString, out DeviceModel model, out DeviceGeneration generation)
        {
            switch (modelString)
            {
                case "M8513":
                case "M8541":
                case "M8697":
                case "M8709":
                    model = DeviceModel.Regular;
                    generation = DeviceGeneration.First;
                    break;
                case "M8737":
                case "M8740":
                case "M8738":
                case "M8741":
                    model = DeviceModel.Regular;
                    generation = DeviceGeneration.Second;
                    break;
                case "M8976":
                case "M8946":
                case "M9460":
                case "M9244":
                case "M8948":
                case "M9245":
                    model = DeviceModel.Regular;
                    generation = DeviceGeneration.Third;
                    break;
                case "M9282":
                case "M9787":
                case "M9268":
                case "MA079":
                case "MA127":
                case "ME436": //HP iPod
                    model = DeviceModel.Regular;
                    generation = DeviceGeneration.Fourth;
                    break;
                case "M9160":
                    model = DeviceModel.Mini;
                    generation = DeviceGeneration.First;
                    break;
                case "M9436":
                    model = DeviceModel.MiniBlue;
                    generation = DeviceGeneration.First;
                    break;
                case "M9435":
                    model = DeviceModel.MiniPink;
                    generation = DeviceGeneration.First;
                    break;
                case "M9434":
                    model = DeviceModel.MiniGreen;
                    generation = DeviceGeneration.First;
                    break;
                case "M9437":
                    model = DeviceModel.MiniGold;
                    generation = DeviceGeneration.First;
                    break;
                case "M9800":
                case "M9801":
                    model = DeviceModel.Mini;
                    generation = DeviceGeneration.Second;
                    break;
                case "M9802":
                case "M9803":
                    model = DeviceModel.MiniBlue;
                    generation = DeviceGeneration.Second;
                    break;
                case "M9804":
                case "M9805":
                    model = DeviceModel.MiniPink;
                    generation = DeviceGeneration.Second;
                    break;
                case "M9806":
                case "M9807":
                    model = DeviceModel.MiniGreen;
                    generation = DeviceGeneration.Second;
                    break;
                case "M9829":
                case "M9585":
                case "M9586":
                case "M9830":
                    model = DeviceModel.Color;
                    generation = DeviceGeneration.Fourth;
                    break;
                case "M9724":
                case "M9725":
                    model = DeviceModel.Shuffle;
                    generation = DeviceGeneration.First;
                    break;
                case "MA546":
                    model = DeviceModel.ShuffleSilver;
                    generation = DeviceGeneration.Second;
                    break;
                case "MA947":
                    model = DeviceModel.ShufflePink;
                    generation = DeviceGeneration.Second;
                    break;
                case "MA949":
                    model = DeviceModel.ShuffleBlue;
                    generation = DeviceGeneration.Second;
                    break;
                case "MA951":
                    model = DeviceModel.ShuffleGreen;
                    generation = DeviceGeneration.Second;
                    break;
                case "MA953":
                    model = DeviceModel.ShuffleOrange;
                    generation = DeviceGeneration.Second;
                    break;
                case "MA350":
                case "MA004":
                case "MA005":
                    model = DeviceModel.NanoWhite;
                    generation = DeviceGeneration.First;
                    break;
                case "MA352":
                case "MA009":
                case "MA107":
                    model = DeviceModel.NanoBlack;
                    generation = DeviceGeneration.First;
                    break;
                case "MA477":
                case "MA426":
                    model = DeviceModel.NanoSilver;
                    generation = DeviceGeneration.Second;
                    break;
                case "MA428":
                    model = DeviceModel.NanoBlue;
                    generation = DeviceGeneration.Second;
                    break;
                case "MA487":
                    model = DeviceModel.NanoGreen;
                    generation = DeviceGeneration.Second;
                    break;
                case "MA489":
                    model = DeviceModel.NanoPink;
                    generation = DeviceGeneration.Second;
                    break;
                case "MA725":
                case "MA726":
                    model = DeviceModel.NanoProductRed;
                    generation = DeviceGeneration.Second;
                    break;
                case "MA497":
                    model = DeviceModel.NanoBlack;
                    generation = DeviceGeneration.Second;
                    break;
                case "MA002":
                case "MA003":
                case "MA444":
                    model = DeviceModel.VideoWhite;
                    generation = DeviceGeneration.Fifth;
                    break;
                case "MA146":
                case "MA147":
                case "MA448":
                case "MA450": /* Possibly Video U2? */
                case "MA446": /* Video U2 */
                    model = DeviceModel.VideoBlack;
                    generation = DeviceGeneration.Fifth;
                    break;
                default:
                    model = DeviceModel.Unknown;
                    generation = DeviceGeneration.Unknown;
                    break;
            }
        }
    }
}
