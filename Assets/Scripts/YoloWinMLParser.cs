/*
    Calculations are based on official YOLO paper
    YOLO9000: Better, Faster, Stronger
    Joseph Redmon, Ali Farhadi
    https://arxiv.org/abs/1612.08242
*/

using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Yolo
{
    internal class YoloWinMlParser : MonoBehaviour
    {
        public const int RowCount = 13;
        public const int ColCount = 13;
        public const int ChannelCount = 125;
        public const int BoxesPerCell = 5;
        public const int BoxInfoFeatureCount = 5;
        public const int ClassCount = 20;
        public float Cellwidth = 32;
        public float Cellheight = 32;

        private int _channelStride = RowCount * ColCount;

        private float[] _anchors = { 1.08F, 1.19F, 3.42F, 4.41F, 6.63F, 11.38F, 9.42F, 5.11F, 16.62F, 10.52F };

        private string[] _labels = {
                "aeroplane", "bicycle", "bird", "boat", "bottle",
                "bus", "car", "cat", "chair", "cow",
                "diningtable", "dog", "horse", "motorbike", "person",
                "pottedplant", "sheep", "sofa", "train", "tvmonitor"
            };


        /// <summary>
        /// Parses Yolo Output
        /// </summary>
        /// <param name="yoloModelOutputs">Vector of Yolo v2 outputs</param>
        /// <param name="framewidth">Width of the frame (ignored if normalized = true)</param>
        /// <param name="frameheight">Height of the frame (ignored if normalized = true) </param>
        /// <param name="threshold">Detection threshold</param>
        /// <returns></returns>
        public IList<YoloBoundingBox> ParseOutputs(float [] yoloModelOutputs, int framewidth, int frameheight, float threshold = .3F)
        {
            var boxes = new List<YoloBoundingBox>();
            Cellheight = frameheight / RowCount;
            Cellwidth = framewidth / ColCount;

            for (var cy = 0; cy < RowCount; cy++)
            {
                for (var cx = 0; cx < ColCount; cx++)
                {
                    for (var b = 0; b < BoxesPerCell; b++)
                    {
                        var channel = (b * (ClassCount + BoxInfoFeatureCount));

                        var tx = yoloModelOutputs[GetOffset(cx, cy, channel)];
                        var ty = yoloModelOutputs[GetOffset(cx, cy, channel + 1)];
                        var tw = yoloModelOutputs[GetOffset(cx, cy, channel + 2)];
                        var th = yoloModelOutputs[GetOffset(cx, cy, channel + 3)];
                        var tc = yoloModelOutputs[GetOffset(cx, cy, channel + 4)];

                        var x = ((float)cx + Sigmoid(tx)) * Cellwidth;
                        var y = ((float)cy + Sigmoid(ty)) * Cellheight;
                        var width = (float)Math.Exp(tw) * Cellwidth * _anchors[b * 2];
                        var height = (float)Math.Exp(th) * Cellheight * _anchors[b * 2 + 1];

                        var confidence = Sigmoid(tc);

                        if (confidence < threshold)
                            continue;

                        var classes = new float[ClassCount];
                        var classOffset = channel + BoxInfoFeatureCount;

                        for (var i = 0; i < ClassCount; i++)
                            classes[i] = yoloModelOutputs[GetOffset(cx, cy, i + classOffset)];

                        var results = Softmax(classes)
                            .Select((v, ik) => new { Value = v, Index = ik });

                        var topClass = results.OrderByDescending(r => r.Value).First().Index;
                        var topScore = results.OrderByDescending(r => r.Value).First().Value * confidence;

                        if (topScore < threshold)
                            continue;

                        boxes.Add(new YoloBoundingBox()
                        {
                            Confidence = topScore,
                            X = (x - width / 2),
                            Y = (y - height / 2),
                            Width = width,
                            Height = height,
                            Label = _labels[topClass]
                        });
                    }
                }
            }

            return boxes;
        }

        public IList<YoloBoundingBox> NonMaxSuppress(IList<YoloBoundingBox> boxes, int limit = 5, float threshold = 0.8f)
        {
            var activeCount = boxes.Count;
            var isActiveBoxes = new bool[boxes.Count];

            for (var i = 0; i < isActiveBoxes.Length; i++)
                isActiveBoxes[i] = true;

            var sortedBoxes = boxes.Select((b, i) => new { Box = b, Index = i })
                                .OrderByDescending(b => b.Box.Confidence)
                                .ToList();

            var results = new List<YoloBoundingBox>();

            for (var i = 0; i < boxes.Count; i++)
            {
                if (isActiveBoxes[i])
                {
                    var boxA = sortedBoxes[i].Box;
                    results.Add(boxA);

                    if (results.Count >= limit)
                        break;

                    for (var j = i + 1; j < boxes.Count; j++)
                    {
                        if (isActiveBoxes[j])
                        {
                            var boxB = sortedBoxes[j].Box;

                            if (IntersectionOverUnion(boxA.Rect, boxB.Rect) > threshold)
                            {
                                isActiveBoxes[j] = false;
                                activeCount--;

                                if (activeCount <= 0)
                                    break;
                            }
                        }
                    }

                    if (activeCount <= 0)
                        break;
                }
            }

            return results;
        }

        private float IntersectionOverUnion(Rect a, Rect b)
        {
            var areaA = a.width * a.height;

            if (areaA <= 0)
                return 0;

            var areaB = b.width * b.height;

            if (areaB <= 0)
                return 0;

            var minX = Math.Max(a.xMin, b.xMin);
            var minY = Math.Max(a.yMin, b.yMin);
            var maxX = Math.Min(a.xMax, b.xMax);
            var maxY = Math.Min(a.yMax, b.yMax);

            var intersectionArea = Math.Max(maxY - minY, 0) * Math.Max(maxX - minX, 0);

            return intersectionArea / (areaA + areaB - intersectionArea);
        }

        private int GetOffset(int x, int y, int channel)
        {
            // YOLO outputs a tensor that has a shape of 125x13x13, which 
            // WinML flattens into a 1D array.  To access a specific channel 
            // for a given (x,y) cell position, we need to calculate an offset
            // into the array
            return (channel * _channelStride) + (y * ColCount) + x;
        }

        private float Sigmoid(float value)
        {
            var k = (float)Math.Exp(value);

            return k / (1.0f + k);
        }

        private float[] Softmax(float[] values)
        {
            var maxVal = values.Max();
            var exp = values.Select(v => Math.Exp(v - maxVal));
            var sumExp = exp.Sum();

            return exp.Select(v => (float)(v / sumExp)).ToArray();
        }
    }
}