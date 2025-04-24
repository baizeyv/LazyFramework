using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Lazy;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using Log = Lazy.Log;
using Random = System.Random;

namespace LazyEditor
{
    public static class SpiderEditor
    {
        [MenuItem("Spider/Test Print")]
        public static void PrintTable()
        {
            var str = "TNRSQW";
            for (var i = 0; i < str.Length; i++)
            {
                var a = VitaCharToCardValue(str[i]);
                Debug.Log(a);
            }
            // var header = string.Format("{0,-10} | {1,-10} | {2,-10}", "Name", "Score", "Level");
            // var separator = new string('-', header.Length);
            //
            // var row1 = string.Format("{0,-10} | {1,-10} | {2,-10}", "Alice", 1200, 5);
            // var row2 = string.Format("{0,-10} | {1,-10} | {2,-10}", "Bob", 980, 4);
            // var row3 = string.Format("{0,-10} | {1,-10} | {2,-10}", "Charlie", 1500, 6);
            //
            // // Log.MsgD("\n" + header + "\n" + separator + "\n" + row1 + "\n" + row2 + "\n" + row3);
            // UnityEngine.Debug.Log(
            //     "\n" + header + "\n" + separator + "\n" + row1 + "\n" + row2 + "\n" + row3
            // );
        }

        [MenuItem("Spider/Export Spider ExportOne")]
        public static void ConvertToCsvOne()
        {
            const string saveFile = @"C:\Users\baizeyv\Documents\a\ExportOne.csv";
            const string filePath = @"C:\Users\baizeyv\Documents\a\ExportOne.txt";
            Convert(filePath, saveFile);
        }

        [MenuItem("Spider/Export Spider ExportTwo")]
        public static void ConvertToCsvTwo()
        {
            const string saveFile = @"C:\Users\baizeyv\Documents\a\ExportTwo.csv";
            const string filePath = @"C:\Users\baizeyv\Documents\a\ExportTwo.txt";
            Convert(filePath, saveFile);
        }

        private static void Convert(string filePath, string saveFile)
        {
            FileUtility.CheckFileAndCreateDirWhenNeeded(saveFile);

            var sb = new StringBuilder();

            using (StreamWriter writer = new(saveFile))
            {
                writer.WriteLine(
                    "id,seed,calc,difficulty,step1,step2,step3,step4,step5,step6,step7,step8,suitCount"
                );
                var id = 0;
                using (StreamReader reader = new(filePath))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        id++;
                        var data = JsonConvert.DeserializeObject<LevelData>(line);
                        sb.Append(id)
                            .Append(",")
                            .Append(data.seed)
                            .Append(",")
                            .Append(data.calc)
                            .Append(",")
                            .Append(data.difficulty)
                            .Append(",")
                            .Append(data.groupStep[0])
                            .Append(",")
                            .Append(data.groupStep[1])
                            .Append(",")
                            .Append(data.groupStep[2])
                            .Append(",")
                            .Append(data.groupStep[3])
                            .Append(",")
                            .Append(data.groupStep[4])
                            .Append(",")
                            .Append(data.groupStep[5])
                            .Append(",")
                            .Append(data.groupStep[6])
                            .Append(",")
                            .Append(data.groupStep[7])
                            .Append(",")
                            .Append(data.suitCount);
                        writer.WriteLine(sb.ToString());
                        sb.Clear();
                    }
                }
            }
        }

        [MenuItem("Spider/Export PlayValve Cards")]
        public static void ExportPlayValveCards()
        {
            List<int> winnable1Suit =
                new()
                {
                    81983,
                    96620,
                    3128,
                    29490,
                    55470,
                    55066,
                    75388,
                    24434,
                    59542,
                    97081,
                    47092,
                    47533,
                    78597,
                    90134,
                    99560,
                    55638,
                    56781,
                    27038,
                    59703,
                    73736,
                    55606,
                    56817,
                    15716,
                    49686,
                    26752,
                    63285,
                    87149,
                    53739,
                    97332,
                    57435,
                    53231,
                    89196,
                    51544,
                    98420,
                    6882,
                    49156,
                    83473,
                    7378,
                    77097,
                    7269,
                    99412,
                    67444,
                    76398,
                    30914,
                    11925,
                    44801,
                    49871,
                    6260,
                    32253,
                    63995,
                    40156,
                    81145,
                    97471,
                    13227,
                    85251,
                    73596,
                    64634,
                    24544,
                    32457,
                    72183,
                    75499,
                    78278,
                    25078,
                    62000,
                    63155,
                    48542,
                    53299,
                    69337,
                    15403,
                    63000,
                    76871,
                    40449,
                    79347,
                    81451,
                    56573,
                    48389,
                    49934,
                    23889,
                    87393,
                    94292,
                    1460,
                    55259,
                    57591,
                    8358,
                    61879,
                    44288,
                    69976,
                    47210,
                    65560,
                    70481,
                    10197,
                    21030,
                    11798,
                    14849,
                    35759,
                    44598,
                    52013,
                    66286,
                    38166,
                    16048,
                    56850,
                    58869,
                    44531,
                    2587,
                    16357,
                    84630,
                    96410,
                    51489,
                    61299,
                    76583,
                    26131,
                    48578,
                    65962,
                    81200,
                    1195,
                    39739,
                    38058,
                    13133,
                    93544,
                    67079,
                    82823,
                    32058,
                    46343,
                    18287,
                    16739,
                    76836,
                    17592,
                    67183,
                    12735,
                    89269,
                    34703,
                    75984,
                    74097,
                    90217,
                    13028,
                    96734,
                    58700,
                    28634,
                    42161,
                    60596,
                    20820,
                    50823,
                    1393,
                    89955,
                    39096,
                    1474,
                    1556,
                    13588,
                    78336,
                    84977,
                    62959,
                    20376,
                    61665,
                    69989,
                    41179,
                    8158,
                    56148,
                    15279,
                    49949,
                    71272,
                    88784,
                    66142,
                    90063,
                    17966,
                    60608,
                    49621,
                    2856,
                    66125,
                    92989,
                    28294,
                    12636,
                    55245,
                    34284,
                    17171,
                    29049,
                    14920,
                    72552,
                    64315,
                    6812,
                    73523,
                    20371,
                    32143,
                    85513,
                    39222,
                    42231,
                    24064,
                    52903,
                    97670,
                    35119,
                    18917,
                    23683,
                    21749,
                    54272,
                    16939,
                    34561,
                    92146,
                    22499,
                    58682,
                    31150,
                    86777,
                    2058,
                    88101,
                    49720,
                    11957,
                    18618,
                    53291,
                    73919,
                    95291,
                    10339,
                    35709,
                    28467,
                    53090,
                    10137,
                    13327,
                    67023,
                    81829,
                    3462,
                    30512,
                    34836,
                    72927,
                    2317,
                    21776,
                    3129,
                    89540,
                    39798,
                    82911,
                    43856,
                    26828,
                    99012,
                    16912,
                    43974,
                    51539,
                    9596,
                    13566,
                    58061,
                    22581,
                    7420,
                    16825,
                    47735,
                    48934,
                    93632,
                    7292,
                    72868,
                    74315,
                    62409,
                    2706,
                    64897,
                    69601,
                    43363,
                    72843,
                    46757,
                    74594,
                    35745,
                    85901,
                    55279,
                    90248,
                    44405,
                    73712,
                    59260,
                    59562,
                    41750,
                    19614,
                    40456,
                    68438,
                    31116,
                    57186,
                    25021,
                    86019,
                    64741,
                    5289,
                    72962,
                    85016,
                    96526,
                    18045,
                    16439,
                    75132,
                    29212,
                    19103,
                    74194,
                    97944,
                    32731,
                    98557,
                    18234,
                    93089,
                    62819,
                    26038,
                    37070,
                    20933,
                    6335,
                    39352,
                    92936,
                    58739,
                    59511,
                    30875,
                    65369,
                    16469,
                    64454,
                    11269,
                    73291,
                    8724,
                    7156,
                    2758,
                    45150,
                    55447,
                    10753,
                    59891,
                    71114,
                    65412,
                    84790,
                    66088,
                    73614,
                    42687,
                    55416,
                    28063,
                    17024,
                    99276,
                    19613,
                    64077,
                    31387,
                    14711,
                    30694,
                    54276,
                    87725,
                    37616,
                    85484,
                    13916,
                    62996,
                    32766,
                    28640,
                    95330,
                    46697,
                    75523,
                    40820,
                    76476,
                    88122,
                    5608,
                    74172,
                    60779,
                    22522,
                    27403,
                    37949,
                    45697,
                    27639,
                    71129,
                    94447,
                    46354,
                    38474,
                    59979,
                    11051,
                    37742,
                    72187,
                    5742,
                    11840,
                    16485,
                    80392,
                    28899,
                    60080,
                    40722,
                    91850,
                    86462,
                    51641,
                    76346,
                    15121,
                    56089,
                    21380,
                    61434,
                    38008,
                    83625,
                    62988,
                    34537,
                    9900,
                    85941,
                    89123,
                    27224,
                    22152,
                    8282,
                    14117,
                    6853,
                    87401,
                    14809,
                    37785,
                    69589,
                    40004,
                    45107,
                    51025,
                    86591,
                    49703,
                    31454,
                    95940,
                    28400,
                    4412,
                    10926,
                    59359,
                    62459,
                    69724,
                    82580,
                    63229,
                    22773,
                    10346,
                    79330,
                    34517,
                    91965,
                    54973,
                    10457,
                    97450,
                    82501,
                    99021,
                    51645,
                    78832,
                    95361,
                    97922,
                    10068,
                    13140,
                    23011,
                    32540,
                    14479,
                    62397,
                    40792,
                    10494,
                    14422,
                    88499,
                    43204,
                    80510,
                    81657,
                    99141,
                    5066,
                    7764,
                    84157,
                    75252,
                    85173,
                    16361,
                    62308,
                    51700,
                    94020,
                    84254,
                    79709,
                    54500,
                    18645,
                    56892,
                    10060,
                    44782,
                    31062,
                    10234,
                    80834,
                    97484,
                    62701,
                    76350,
                    65863,
                    57023,
                    71919,
                    13682,
                    33097,
                    49705,
                    22337,
                    13915,
                    47114,
                    60461,
                    48247,
                    6629,
                    63629,
                    67269,
                    87605,
                    32331,
                    75288,
                    54916,
                    55602,
                    14651,
                    86593,
                    8364,
                    62977,
                    80260,
                    13537,
                    5964,
                    2557,
                    45660,
                    70170,
                    64194,
                    39843,
                    53986,
                    66490,
                    25992,
                    80303,
                    33744,
                    15345,
                    75804,
                };
            List<int> winnable1Suit8Max =
                new()
                {
                    94048,
                    92464,
                    93452,
                    45847,
                    76536,
                    95771,
                    44697,
                    54340,
                    62813,
                    13487,
                    68823,
                    68978,
                    59859,
                    29298,
                    9896,
                    13381,
                    84597,
                    87240,
                    18394,
                    71483,
                    53304,
                    97027,
                    8794,
                    73356,
                    9006,
                    18089,
                    35409,
                    31230,
                    4138,
                    72689,
                    66664,
                    33169,
                    48297,
                    54100,
                    55610,
                    21467,
                    44708,
                    57805,
                    36479,
                    12095,
                    76644,
                    6779,
                    48546,
                    80241,
                    68554,
                    6047,
                    3330,
                    42161,
                    74680,
                    69328,
                    81407,
                    73442,
                    14561,
                    39610,
                    20388,
                    74010,
                    41279,
                    18036,
                    50963,
                    77758,
                    52594,
                    73841,
                    41263,
                    3048,
                    12022,
                    38808,
                    45653,
                    14000,
                    76335,
                    14513,
                    19234,
                    3711,
                    52129,
                    44906,
                    96191,
                    39939,
                    73236,
                    80961,
                    28033,
                    57421,
                    12340,
                    90767,
                    51394,
                    50805,
                    69320,
                    68392,
                    74584,
                    45678,
                    29938,
                    84020,
                    64266,
                    16055,
                    48136,
                    10590,
                    46380,
                    45081,
                    43317,
                    37718,
                    43837,
                    18482,
                };
            List<int> winnable1Suit10Max =
                new()
                {
                    77043,
                    22795,
                    33401,
                    86543,
                    3778,
                    27845,
                    66724,
                    82399,
                    36571,
                    48612,
                    48676,
                    56519,
                    55157,
                    18615,
                    22983,
                    67700,
                    60903,
                    62670,
                    64292,
                    84770,
                    17504,
                    84753,
                    96387,
                    67370,
                    7749,
                    61275,
                    33769,
                    20482,
                    87148,
                    68133,
                    71943,
                    76598,
                    33403,
                    70418,
                    52467,
                    9467,
                    50788,
                    65680,
                    5840,
                    77213,
                    77519,
                    45799,
                    54951,
                    86601,
                    62731,
                    93312,
                    98461,
                    40576,
                    10154,
                    84572,
                    63752,
                    70880,
                    98204,
                    32077,
                    4752,
                    8868,
                    30082,
                    56916,
                    48927,
                    93239,
                    12380,
                    93375,
                    25731,
                    55133,
                    94850,
                    88429,
                    36126,
                    88653,
                    7269,
                    90462,
                    32728,
                    34918,
                    44862,
                    77909,
                    11487,
                    45598,
                    43849,
                    44765,
                    98973,
                    6104,
                    90654,
                    6390,
                    12504,
                    70976,
                    92249,
                    35337,
                    49436,
                    63973,
                    33789,
                    32638,
                    23972,
                    35589,
                    34381,
                    65433,
                    31365,
                    48459,
                    81827,
                    43863,
                    41695,
                    3217,
                };
            List<int> winnable2Suits =
                new()
                {
                    3428,
                    81834,
                    46550,
                    79764,
                    37091,
                    59844,
                    34162,
                    10901,
                    25457,
                    21407,
                    48434,
                    10148,
                    74454,
                    3022,
                    92241,
                    36535,
                    53721,
                    10672,
                    56517,
                    63335,
                    28861,
                    52206,
                    41551,
                    21211,
                    5914,
                    50829,
                    19666,
                    20574,
                    93571,
                    51371,
                    63558,
                    76061,
                    70368,
                    44198,
                    85648,
                    61121,
                    66121,
                    17988,
                    91105,
                    36165,
                    55247,
                    52867,
                    53357,
                    83021,
                    42813,
                    23955,
                    3771,
                    10664,
                    4917,
                    28046,
                    10811,
                    87331,
                    61465,
                    45492,
                    85529,
                    10300,
                    96939,
                    87222,
                    75549,
                    12469,
                    72134,
                    45955,
                    95307,
                    79226,
                    7660,
                    65484,
                    83659,
                    83314,
                    60193,
                    10887,
                    66886,
                    47500,
                    40592,
                    69612,
                    79342,
                    65146,
                    44945,
                    29514,
                    32977,
                    29704,
                    44981,
                    46925,
                    77596,
                    45910,
                    16380,
                    77283,
                    97728,
                    16797,
                    44484,
                    68563,
                    63575,
                    38975,
                    3164,
                    50377,
                    27513,
                    2304,
                    30151,
                    9229,
                    33865,
                    7871,
                };
            List<int> winnable3Suits =
                new()
                {
                    54449,
                    78947,
                    86762,
                    13490,
                    36653,
                    13135,
                    85057,
                    3112,
                    86761,
                    64635,
                    32312,
                    57510,
                    20557,
                    85409,
                    38790,
                    61358,
                    83343,
                    90613,
                    75679,
                    2349,
                    43599,
                    7852,
                    77842,
                    36655,
                    87360,
                    56077,
                    76746,
                    25994,
                    28154,
                    97136,
                    24156,
                    17036,
                    81476,
                    49642,
                    82576,
                    76266,
                    74836,
                    77968,
                    76266,
                    30565,
                    7259,
                    49131,
                    30311,
                    94429,
                    59774,
                    55010,
                    35999,
                    40422,
                    55036,
                    57646,
                    68518,
                    52936,
                    77582,
                    89067,
                    57085,
                    55552,
                    47411,
                    1289,
                    59105,
                    89125,
                    24051,
                    80730,
                    25015,
                    8843,
                    32312,
                    7903,
                    52583,
                    46423,
                    21284,
                    93107,
                    57629,
                    80297,
                    82339,
                    17081,
                    65835,
                    36005,
                    45799,
                    82362,
                    94600,
                    36151,
                    75332,
                    75882,
                    89067,
                    55423,
                    41687,
                    27172,
                    44194,
                    14325,
                    47347,
                    78370,
                    14923,
                    3730,
                    38350,
                    9625,
                    45629,
                    55957,
                    53169,
                    19307,
                    97013,
                    79230,
                };
            List<int> winnable4Suits =
                new()
                {
                    75130,
                    71074,
                    42820,
                    41174,
                    75861,
                    65617,
                    98226,
                    87532,
                    7906,
                    21132,
                    54287,
                    37014,
                    83977,
                    51458,
                    62412,
                    59466,
                    14334,
                    49971,
                    33155,
                    64916,
                    96742,
                    16546,
                    39768,
                    41051,
                    41174,
                    41378,
                    48643,
                    67066,
                    65016,
                    48506,
                    50777,
                    6863,
                    7611,
                    23536,
                    343590,
                    251522,
                    848763,
                    247835,
                    194760,
                    889952,
                    315960,
                    973754,
                    835565,
                    320754,
                    876058,
                    845253,
                    206996,
                    310399,
                    849954,
                    325192,
                };

            const string winnable1SuitSaveFile = @"C:\Users\baizeyv\Documents\a\winnable1Suit.txt";
            const string winnable1Suit8MaxSaveFile =
                @"C:\Users\baizeyv\Documents\a\winnable1Suit8Max.txt";
            const string winnable1Suit10MaxSaveFile =
                @"C:\Users\baizeyv\Documents\a\winnable1Suit10Max.txt";
            const string winnable2SuitsSaveFile =
                @"C:\Users\baizeyv\Documents\a\winnable2Suits.txt";
            const string winnable3SuitsSaveFile =
                @"C:\Users\baizeyv\Documents\a\winnable3Suits.txt";
            const string winnable4SuitsSaveFile =
                @"C:\Users\baizeyv\Documents\a\winnable4Suits.txt";
            GeneratePlayValveTxt(winnable1Suit, winnable1SuitSaveFile);
            GeneratePlayValveTxt(winnable1Suit8Max, winnable1Suit8MaxSaveFile);
            GeneratePlayValveTxt(winnable1Suit10Max, winnable1Suit10MaxSaveFile);
            GeneratePlayValveTxt(winnable2Suits, winnable2SuitsSaveFile);
            GeneratePlayValveTxt(winnable3Suits, winnable3SuitsSaveFile);
            GeneratePlayValveTxt(winnable4Suits, winnable4SuitsSaveFile);
        }

        private static void GeneratePlayValveTxt(List<int> seedList, string saveFile)
        {
            using (var writer = new StreamWriter(saveFile, true))
            {
                StringBuilder sb = new();
                foreach (var seed in seedList)
                {
                    var cards = GeneratePlayValveCards(seed);
                    sb.Append(seed).Append(",");
                    sb.Append(string.Join(",", cards));
                    writer.WriteLine(sb.ToString());
                    sb.Clear();
                }
            }
        }

        private static List<int> GeneratePlayValveCards(int seed)
        {
            var random = new Random(seed);
            var cards = new List<int>();
            for (var i = 0; i < 13; i++)
            for (var j = 1; j <= 8; j++)
                cards.Add(i + 1);
            var result = cards.OrderBy(_ => random.Next()).ToList();
            //print
            var xx = "";
            foreach (var q in result)
                xx += q + " ";
            Log.MsgD(xx);
            return result;
        }

        private static void ReadPlayValveSeed(int seed)
        {
            var random = new Random(seed);
            var cards = new List<int>();
            for (var i = 0; i < 13; i++)
            for (var j = 1; j <= 8; j++)
                cards.Add(i + 1);
            var result = cards.OrderBy(_ => random.Next()).ToList();
            //print
            var xx = "";
            foreach (var q in result)
                xx += q + " ";
            Log.MsgD(xx);
            //change to vita format
            var k = 0;
            var stacks = new List<List<int>>();
            for (var i = 0; i < 10; i++)
                stacks.Add(new List<int>());
            for (var i = 0; i < 4; i++)
                stacks[i].Add(result[k++]);
            for (var j = 0; j < 5; j++)
            {
                for (var i = 4; i < 10; i++)
                    stacks[i].Add(result[k++]);
                for (var i = 0; i < 4; i++)
                    stacks[i].Add(result[k++]);
            }

            var vitaCode = "";
            for (var i = 0; i < 10; i++)
            {
                var stackStr = "";
                for (var j = 0; j < stacks[i].Count; j++)
                    stackStr += CardValueToChar(stacks[i][j]);
                vitaCode += stackStr + ",0;";
            }

            var deckCards = new List<int>();
            for (var i = result.Count - 1; i >= k; i--)
            {
                vitaCode += CardValueToChar(result[i]);
                deckCards.Add(result[i]);
            }

            ShuffleCards(stacks, deckCards);
            vitaCode += ",0";
            Log.MsgD(vitaCode);
        }

        private static void ShuffleCards(List<List<int>> stacks, List<int> deck)
        {
            //Debug.Log("----be");
            //string xx = "";
            //for (int i = 0; i < 10; i++) {
            //    for(int j = 0; j < stacks[i].Count; j++) {
            //        xx += stacks[i][j] + ",";
            //    }
            //    xx += "#";
            //}
            //Debug.Log(xx);
            for (var i = 0; i < 4; i++)
            {
                var j = UnityEngine.Random.Range(0, 4);
                (stacks[i], stacks[j]) = (stacks[j], stacks[i]);
            }

            for (var i = 4; i < 10; i++)
            {
                var j = UnityEngine.Random.Range(4, 10);
                (stacks[i], stacks[j]) = (stacks[j], stacks[i]);
            }

            for (var i = 0; i < 5; i++)
            for (var j = 0; j < 10; j++)
            {
                var ii = UnityEngine.Random.Range(i * 10, i * 10 + 10);
                var x = deck[i * 10 + j];
                deck[i * 10 + j] = deck[ii];
                deck[i * 10 + j] = x;
            }
            //Debug.Log("----after");
            //xx = "";
            //for (int i = 0; i < 10; i++) {
            //    for (int j = 0; j < stacks[i].Count; j++) {
            //        xx += stacks[i][j] + ",";
            //    }
            //    xx += "#";
            //}
            //Debug.Log(xx);
        }

        //方片A-K（A-M）
        //黑桃A-K（N-Z）
        //梅花A-K（a-m）
        //红桃A-K（n-z）
        //Vita Spider 第一关: "TNRSQW,1;RYXOXP,1;WQQWOW,1;ZRXPNZ,1;SVTRP,1;VOXYY,1;NOYZV,1;PUZXO,1;PUWPS,1;WRSYU,1;XYUTNZZOUNSVXVOQPZSVZWRQUTRRQNSQXVUSTWPUTNTNQVYOYT,0",
        //第一列从下往上是：TNRSQW
        //排堆的从下往上是：XYUTNZZOUNSVXVOQPZSVZWRQUTRRQNSQXVUSTWPUTNTNQVYOYT可以理解最后一个放在最上面，会最先发下去

        /// <summary>
        /// * 牌面值转换为字符
        /// # 黑桃1-13，红桃14-26，梅花27-39，方片40-54
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        private static char CardValueToChar(int x)
        {
            if (x < 14)
                return (char)('N' + (x - 1));
            if (x < 27)
                return (char)('n' + (x - 14));
            if (x < 40)
                return (char)('a' + (x - 27));
            return (char)('A' + (x - 40));
        }

        /// <summary>
        /// * Vita的关卡中的牌值转为自己的Poker的值
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        private static int VitaCharToCardValue(char c)
        {
            var v4 = c - 'N' + 1;
            if (v4 is >= 1 and <= 54)
                // # 有效值
                return v4;
            var v3 = c - 'n' + 14;
            if (v3 is >= 1 and <= 54)
                // # 有效值
                return v3;
            var v2 = c - 'a' + 27;
            if (v2 is >= 1 and <= 54)
                // # 有效值
                return v2;
            var v1 = c - 'A' + 40;
            if (v1 is >= 1 and <= 54)
                // # 有效值
                return v1;
            return 0;
        }

        private static void VitaLevelConvertToPoker(string vitaLevel)
        {
            var array = vitaLevel.Split(",1;");
            var deck = string.Empty;
            // # 牌堆
            var deckString = array[^1].Substring(0, array[^1].Length - 2); // # -2 是为了去掉最后的,0
            for (var i = deckString.Length - 1; i >= 0; i--)
                deck += deckString[i];

            var value = string.Empty;
            var idx = 0;
            // # 遍历10列
            for (var x = 0; x < 6; x++)
            {
                for (var i = 0; i < 10; i++)
                {
                    var str = array[i];
                    if (str.Length <= idx)
                        continue;
                    var c = str[str.Length - 1 - idx];
                    value += c;
                }

                idx++;
            }

            var s = value + deck;
            var output = string.Empty;
            for (var i = 0; i < s.Length; i++)
            {
                var val = VitaCharToCardValue(s[i]);
                output += val + ",";
            }

            Debug.Log(output);
        }
    }

    public static class SpiderEnumerableExtensions
    {
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source, Random rng)
        {
            return source
                .Select((value, index) => new { value, index })
                .OrderBy(x => x.index) // Reset index for stable sort
                .ThenBy(_ => rng.Next()) // Shuffle based on random values
                .Select(x => x.value); // Retrieve the value
        }
    }

    [Serializable]
    public class LevelData
    {
        /// <summary>
        /// * 求解次数
        /// </summary>
        public int calc;

        /// <summary>
        /// * 牌面
        /// </summary>
        public int[] cards;

        /// <summary>
        /// * 难度
        /// </summary>
        public float difficulty;

        /// <summary>
        /// * 收每一套牌的步骤
        /// </summary>
        public int[] groupStep;

        /// <summary>
        /// * Cpp 种子
        /// </summary>
        public long seed;

        /// <summary>
        /// * 最优求解步骤
        /// </summary>
        public int step;

        /// <summary>
        /// * 花色
        /// </summary>
        public int suitCount;
    }
}
