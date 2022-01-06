using TinyParserMitsuba;
using System;
using System.Linq;

namespace Mitsuba2SeeSharp {
    class Conductors {
        // AG
        private static float[] AG_Wvls = new float[] { 298.757050f, 302.400421f, 306.133759f, 309.960449f, 313.884003f, 317.908142f, 322.036835f, 326.274139f, 330.624481f, 335.092377f, 339.682678f, 344.400482f, 349.251221f, 354.240509f, 359.374420f, 364.659332f, 370.102020f, 375.709625f, 381.489777f, 387.450562f, 393.600555f, 399.948975f, 406.505493f, 413.280579f, 420.285339f, 427.531647f, 435.032196f, 442.800629f, 450.851562f, 459.200653f, 467.864838f, 476.862213f, 486.212463f, 495.936707f, 506.057861f, 516.600769f, 527.592224f, 539.061646f, 551.040771f, 563.564453f, 576.670593f, 590.400818f, 604.800842f, 619.920898f, 635.816284f, 652.548279f, 670.184753f, 688.800964f, 708.481018f, 729.318665f, 751.419250f, 774.901123f, 799.897949f, 826.561157f, 855.063293f, 885.601257f };
        private static float[] AG_K = new float[] { 1.080000f, 0.882000f, 0.761063f, 0.647000f, 0.550875f, 0.504000f, 0.554375f, 0.663000f, 0.818563f, 0.986000f, 1.120687f, 1.240000f, 1.345250f, 1.440000f, 1.533750f, 1.610000f, 1.641875f, 1.670000f, 1.735000f, 1.810000f, 1.878750f, 1.950000f, 2.029375f, 2.110000f, 2.186250f, 2.260000f, 2.329375f, 2.400000f, 2.478750f, 2.560000f, 2.640000f, 2.720000f, 2.798125f, 2.880000f, 2.973750f, 3.070000f, 3.159375f, 3.250000f, 3.348125f, 3.450000f, 3.553750f, 3.660000f, 3.766250f, 3.880000f, 4.010625f, 4.150000f, 4.293125f, 4.440000f, 4.586250f, 4.740000f, 4.908125f, 5.090000f, 5.288750f, 5.500000f, 5.720624f, 5.950000f };
        private static float[] AG_Eta = new float[] { 1.519000f, 1.496000f, 1.432500f, 1.323000f, 1.142062f, 0.932000f, 0.719062f, 0.526000f, 0.388125f, 0.294000f, 0.253313f, 0.238000f, 0.221438f, 0.209000f, 0.194813f, 0.186000f, 0.192063f, 0.200000f, 0.198063f, 0.192000f, 0.182000f, 0.173000f, 0.172625f, 0.173000f, 0.166688f, 0.160000f, 0.158500f, 0.157000f, 0.151063f, 0.144000f, 0.137313f, 0.132000f, 0.130250f, 0.130000f, 0.129938f, 0.130000f, 0.130063f, 0.129000f, 0.124375f, 0.120000f, 0.119313f, 0.121000f, 0.125500f, 0.131000f, 0.136125f, 0.140000f, 0.140063f, 0.140000f, 0.144313f, 0.148000f, 0.145875f, 0.143000f, 0.142563f, 0.145000f, 0.151938f, 0.163000f };

        // AL
        private static float[] AL_Wvls = new float[] { 298.757050f, 302.400421f, 306.133759f, 309.960449f, 313.884003f, 317.908142f, 322.036835f, 326.274139f, 330.624481f, 335.092377f, 339.682678f, 344.400482f, 349.251221f, 354.240509f, 359.374420f, 364.659332f, 370.102020f, 375.709625f, 381.489777f, 387.450562f, 393.600555f, 399.948975f, 406.505493f, 413.280579f, 420.285339f, 427.531647f, 435.032196f, 442.800629f, 450.851562f, 459.200653f, 467.864838f, 476.862213f, 486.212463f, 495.936707f, 506.057861f, 516.600769f, 527.592224f, 539.061646f, 551.040771f, 563.564453f, 576.670593f, 590.400818f, 604.800842f, 619.920898f, 635.816284f, 652.548279f, 670.184753f, 688.800964f, 708.481018f, 729.318665f, 751.419250f, 774.901123f, 799.897949f, 826.561157f, 855.063293f, 885.601257f };
        private static float[] AL_Eta = new float[] { 0.273375f, 0.280000f, 0.286813f, 0.294000f, 0.301875f, 0.310000f, 0.317875f, 0.326000f, 0.334750f, 0.344000f, 0.353813f, 0.364000f, 0.374375f, 0.385000f, 0.395750f, 0.407000f, 0.419125f, 0.432000f, 0.445688f, 0.460000f, 0.474688f, 0.490000f, 0.506188f, 0.523000f, 0.540063f, 0.558000f, 0.577313f, 0.598000f, 0.620313f, 0.644000f, 0.668625f, 0.695000f, 0.723750f, 0.755000f, 0.789000f, 0.826000f, 0.867000f, 0.912000f, 0.963000f, 1.020000f, 1.080000f, 1.150000f, 1.220000f, 1.300000f, 1.390000f, 1.490000f, 1.600000f, 1.740000f, 1.910000f, 2.140000f, 2.410000f, 2.630000f, 2.800000f, 2.740000f, 2.580000f, 2.240000f };
        private static float[] AL_K = new float[] { 3.593750f, 3.640000f, 3.689375f, 3.740000f, 3.789375f, 3.840000f, 3.894375f, 3.950000f, 4.005000f, 4.060000f, 4.113750f, 4.170000f, 4.233750f, 4.300000f, 4.365000f, 4.430000f, 4.493750f, 4.560000f, 4.633750f, 4.710000f, 4.784375f, 4.860000f, 4.938125f, 5.020000f, 5.108750f, 5.200000f, 5.290000f, 5.380000f, 5.480000f, 5.580000f, 5.690000f, 5.800000f, 5.915000f, 6.030000f, 6.150000f, 6.280000f, 6.420000f, 6.550000f, 6.700000f, 6.850000f, 7.000000f, 7.150000f, 7.310000f, 7.480000f, 7.650000f, 7.820000f, 8.010000f, 8.210000f, 8.390000f, 8.570000f, 8.620000f, 8.600000f, 8.450000f, 8.310000f, 8.210000f, 8.210000f };

        // AU
        private static float[] AU_Wvls = new float[] { 298.757050f, 302.400421f, 306.133759f, 309.960449f, 313.884003f, 317.908142f, 322.036835f, 326.274139f, 330.624481f, 335.092377f, 339.682678f, 344.400482f, 349.251221f, 354.240509f, 359.374420f, 364.659332f, 370.102020f, 375.709625f, 381.489777f, 387.450562f, 393.600555f, 399.948975f, 406.505493f, 413.280579f, 420.285339f, 427.531647f, 435.032196f, 442.800629f, 450.851562f, 459.200653f, 467.864838f, 476.862213f, 486.212463f, 495.936707f, 506.057861f, 516.600769f, 527.592224f, 539.061646f, 551.040771f, 563.564453f, 576.670593f, 590.400818f, 604.800842f, 619.920898f, 635.816284f, 652.548279f, 670.184753f, 688.800964f, 708.481018f, 729.318665f, 751.419250f, 774.901123f, 799.897949f, 826.561157f, 855.063293f, 885.601257f };
        private static float[] AU_Eta = new float[] { 1.795000f, 1.812000f, 1.822625f, 1.830000f, 1.837125f, 1.840000f, 1.834250f, 1.824000f, 1.812000f, 1.798000f, 1.782000f, 1.766000f, 1.752500f, 1.740000f, 1.727625f, 1.716000f, 1.705875f, 1.696000f, 1.684750f, 1.674000f, 1.666000f, 1.658000f, 1.647250f, 1.636000f, 1.628000f, 1.616000f, 1.596250f, 1.562000f, 1.502125f, 1.426000f, 1.345875f, 1.242000f, 1.086750f, 0.916000f, 0.754500f, 0.608000f, 0.491750f, 0.402000f, 0.345500f, 0.306000f, 0.267625f, 0.236000f, 0.212375f, 0.194000f, 0.177750f, 0.166000f, 0.161000f, 0.160000f, 0.160875f, 0.164000f, 0.169500f, 0.176000f, 0.181375f, 0.188000f, 0.198125f, 0.210000f };
        private static float[] AU_K = new float[] { 1.920375f, 1.920000f, 1.918875f, 1.916000f, 1.911375f, 1.904000f, 1.891375f, 1.878000f, 1.868250f, 1.860000f, 1.851750f, 1.846000f, 1.845250f, 1.848000f, 1.852375f, 1.862000f, 1.883000f, 1.906000f, 1.922500f, 1.936000f, 1.947750f, 1.956000f, 1.959375f, 1.958000f, 1.951375f, 1.940000f, 1.924500f, 1.904000f, 1.875875f, 1.846000f, 1.814625f, 1.796000f, 1.797375f, 1.840000f, 1.956500f, 2.120000f, 2.326250f, 2.540000f, 2.730625f, 2.880000f, 2.940625f, 2.970000f, 3.015000f, 3.060000f, 3.070000f, 3.150000f, 3.445812f, 3.800000f, 4.087687f, 4.357000f, 4.610188f, 4.860000f, 5.125813f, 5.390000f, 5.631250f, 5.880000f };

        // CR
        private static float[] CR_Wvls = new float[] { 300.194000f, 307.643005f, 316.276001f, 323.708008f, 333.279999f, 341.542999f, 351.217987f, 362.514984f, 372.312012f, 385.031006f, 396.102020f, 409.175018f, 424.589020f, 438.092010f, 455.808990f, 471.406982f, 490.040009f, 512.314026f, 532.102966f, 558.468018f, 582.066040f, 610.739014f, 700.452026f, 815.658020f, 826.533020f, 849.178040f, 860.971985f, 885.570984f };
        private static float[] CR_Eta = new float[] { 0.980000f, 1.020000f, 1.060000f, 1.120000f, 1.180000f, 1.260000f, 1.330000f, 1.390000f, 1.430000f, 1.440000f, 1.480000f, 1.540000f, 1.650000f, 1.800000f, 1.990000f, 2.220000f, 2.490000f, 2.750000f, 2.980000f, 3.180000f, 3.340000f, 3.480000f, 3.840000f, 4.230000f, 4.270000f, 4.310000f, 4.330000f, 4.380000f };
        private static float[] CR_K = new float[] { 2.670000f, 2.760000f, 2.850000f, 2.950000f, 3.040000f, 3.120000f, 3.180000f, 3.240000f, 3.310000f, 3.400000f, 3.540000f, 3.710000f, 3.890000f, 4.060000f, 4.220000f, 4.360000f, 4.440000f, 4.460000f, 4.450000f, 4.410000f, 4.380000f, 4.360000f, 4.370000f, 4.340000f, 4.330000f, 4.320000f, 4.320000f, 4.310000f };

        // CU
        private static float[] CU_Wvls = new float[] { 302.400421f, 306.133759f, 309.960449f, 313.884003f, 317.908142f, 322.036835f, 326.274139f, 330.624481f, 335.092377f, 339.682678f, 344.400482f, 349.251221f, 354.240509f, 359.374420f, 364.659332f, 370.102020f, 375.709625f, 381.489777f, 387.450562f, 393.600555f, 399.948975f, 406.505493f, 413.280579f, 420.285339f, 427.531647f, 435.032196f, 442.800629f, 450.851562f, 459.200653f, 467.864838f, 476.862213f, 486.212463f, 495.936707f, 506.057861f, 516.600769f, 527.592224f, 539.061646f, 551.040771f, 563.564453f, 576.670593f, 590.400818f, 604.800842f, 619.920898f, 635.816284f, 652.548279f, 670.184753f, 688.800964f, 708.481018f, 729.318665f, 751.419250f, 774.901123f, 799.897949f, 826.561157f, 855.063293f, 885.601257f };
        private static float[] CU_Eta = new float[] { 1.380000f, 1.358438f, 1.340000f, 1.329063f, 1.325000f, 1.332500f, 1.340000f, 1.334375f, 1.325000f, 1.317812f, 1.310000f, 1.300313f, 1.290000f, 1.281563f, 1.270000f, 1.249062f, 1.225000f, 1.200000f, 1.180000f, 1.174375f, 1.175000f, 1.177500f, 1.180000f, 1.178125f, 1.175000f, 1.172812f, 1.170000f, 1.165312f, 1.160000f, 1.155312f, 1.150000f, 1.142812f, 1.135000f, 1.131562f, 1.120000f, 1.092437f, 1.040000f, 0.950375f, 0.826000f, 0.645875f, 0.468000f, 0.351250f, 0.272000f, 0.230813f, 0.214000f, 0.209250f, 0.213000f, 0.216250f, 0.223000f, 0.236500f, 0.250000f, 0.254188f, 0.260000f, 0.280000f, 0.300000f };
        private static float[] CU_K = new float[] { 1.687000f, 1.703313f, 1.720000f, 1.744563f, 1.770000f, 1.791625f, 1.810000f, 1.822125f, 1.834000f, 1.851750f, 1.872000f, 1.894250f, 1.916000f, 1.931688f, 1.950000f, 1.972438f, 2.015000f, 2.121562f, 2.210000f, 2.177188f, 2.130000f, 2.160063f, 2.210000f, 2.249938f, 2.289000f, 2.326000f, 2.362000f, 2.397625f, 2.433000f, 2.469187f, 2.504000f, 2.535875f, 2.564000f, 2.589625f, 2.605000f, 2.595562f, 2.583000f, 2.576500f, 2.599000f, 2.678062f, 2.809000f, 3.010750f, 3.240000f, 3.458187f, 3.670000f, 3.863125f, 4.050000f, 4.239563f, 4.430000f, 4.619563f, 4.817000f, 5.034125f, 5.260000f, 5.485625f, 5.717000f };

        public static (float, float, SeeVector) Lookup(string name) {
            switch (name.ToLower()) {
                case "ag": return LookupReturn(AG_Wvls, AG_Eta, AG_K);
                case "al": return LookupReturn(AL_Wvls, AL_Eta, AL_K);
                case "au": return LookupReturn(AU_Wvls, AU_Eta, AU_K);
                case "cr": return LookupReturn(CR_Wvls, CR_Eta, CR_K);
                case "cu": return LookupReturn(CU_Wvls, CU_Eta, CU_K);
                case "none": return (0, 1, new() { x = 0, y = 0, z = 0 });
                default:
                    Log.Error($"Unknown conductor material '{name}'");
                    goto case "cu";
            }
        }

        private static (float, float, SeeVector) LookupReturn(float[] wvls, float[] eta, float[] kappa) {
            float pick = 540.0f;
            int sx = wvls.Where((float v) => { return v <= pick; }).Count() - 1;
            int ex = wvls.Where((float v) => { return v > pick; }).Count() - 1;

            float ratio = sx == ex ? 1 : (pick - wvls[sx]) / (wvls[ex] - wvls[sx]);
            float e = eta[sx] * (1 - ratio) + eta[ex] * ratio;
            float k = kappa[sx] * (1 - ratio) + kappa[ex] * ratio;
            return (e, k, ComputeRGB(wvls, kappa));
        }
        
        private static SeeVector ComputeRGB(float[] wvls, float[] kappa) {
            float maxKappa = kappa.Max();
            var refl = kappa.Select((float v, int i) => { return v / maxKappa; });

            (float x, float y, float z) = SpectralMapper.Eval(wvls, refl.ToArray(), false);
            (float r, float g, float b) = SpectralMapper.MapRGB(x, y, z);

            return new() { x = r, y = g, z = b };
        }
    }
}