using System;
using System.Collections.Generic;
#if UNITY_WSA && !UNITY_EDITOR && SDK_1809

using System.Threading.Tasks;
using Windows.Media;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.AI.MachineLearning;
   
public sealed class TinyYoloV2O12Input
{
    public ImageFeatureValue image; // shape(-1,3,416,416)
}
    
public sealed class TinyYoloV2O12Output
{
    public TensorFloat grid; // shape(-1,125,13,13)
}
    
public sealed class TinyYoloV2O12Model
{
    private LearningModel model;
    private LearningModelSession session;
    private LearningModelBinding binding;
    public static async Task<TinyYoloV2O12Model> CreateFromStreamAsync(IRandomAccessStreamReference stream)
    {
        TinyYoloV2O12Model learningModel = new TinyYoloV2O12Model();
        learningModel.model = await LearningModel.LoadFromStreamAsync(stream);
        learningModel.session = new LearningModelSession(learningModel.model);
        learningModel.binding = new LearningModelBinding(learningModel.session);
        return learningModel;
    }
    public async Task<TinyYoloV2O12Output> EvaluateAsync(TinyYoloV2O12Input input)
    {
        binding.Bind("image", input.image);
        var result = await session.EvaluateAsync(binding, "0");
        var output = new TinyYoloV2O12Output();
        output.grid = result.Outputs["grid"] as TensorFloat;
        return output;
    }
}

#endif
