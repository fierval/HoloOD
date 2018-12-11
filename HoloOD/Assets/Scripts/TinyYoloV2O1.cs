#if UNITY_WSA && !UNITY_EDITOR && !SDK_1809
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Storage;
using Windows.AI.MachineLearning.Preview;

// TinyYoloV2O1

public sealed class TinyYoloV2O1ModelInput
{
    public VideoFrame image { get; set; }
}

public sealed class TinyYoloV2O1ModelOutput
{
    public List<float> grid { get; set; }
    public TinyYoloV2O1ModelOutput()
    {
        this.grid = new List<float>();
        this.grid.AddRange(new float[125 * 13 * 13]);
    }
}

public sealed class TinyYoloV2O1Model
{
    private LearningModelPreview learningModel;
    public static async Task<TinyYoloV2O1Model> CreateTinyYoloV2O1Model(StorageFile file)
    {
        LearningModelPreview learningModel = await LearningModelPreview.LoadModelFromStorageFileAsync(file);
        TinyYoloV2O1Model model = new TinyYoloV2O1Model();
        model.learningModel = learningModel;
        return model;
    }
    public async Task<TinyYoloV2O1ModelOutput> EvaluateAsync(TinyYoloV2O1ModelInput input) {
        TinyYoloV2O1ModelOutput output = new TinyYoloV2O1ModelOutput();
        LearningModelBindingPreview binding = new LearningModelBindingPreview(learningModel);
        binding.Bind("image", input.image);
        binding.Bind("grid", output.grid);
        LearningModelEvaluationResultPreview evalResult = await learningModel.EvaluateAsync(binding, string.Empty);
        return output;
    }
}
#endif