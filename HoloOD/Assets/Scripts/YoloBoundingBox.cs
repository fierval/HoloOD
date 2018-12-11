using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Yolo
{
    public class YoloBoundingBox
    {
        public string Label { get; set; }
        public float X { get; set; }
        public float Y { get; set; }

        public float Height { get; set; }
        public float Width { get; set; }

        public float Confidence { get; set; }

        public Rect Rect
        {
            get { return new Rect(X, Y, Width, Height); }
        }

        public override string ToString()
        {
            return $"Label: {Label}, X: {X}, Y: {Y}, Height: {Height}, Width: {Width}, Confidence: {Confidence}";
        }

        public static List<YoloBoundingBox> SortByConfidence(IList<YoloBoundingBox> predictions)
        {
            return predictions.OrderBy(p => p.Confidence).ToList();
        }
    }
}