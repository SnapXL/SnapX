// SPDX-License-Identifier: GPL-3.0-or-later


using System.Numerics;
using SixLabors.ImageSharp;

namespace SnapX.Core.Utils;

public static class MathHelpers
{
    public const float RadianPI = 57.29578f; // 180.0 / Math.PI
    public const float DegreePI = 0.01745329f; // Math.PI / 180.0
    public const float TwoPI = 6.28319f; // Math.PI * 2

    public static T Min<T>(T num, T min) where T : IComparable<T>
    {
        if (num.CompareTo(min) > 0) return min;
        return num;
    }

    public static T Max<T>(T num, T max) where T : IComparable<T>
    {
        if (num.CompareTo(max) < 0) return max;
        return num;
    }

    public static T Clamp<T>(T num, T min, T max) where T : IComparable<T>
    {
        if (num.CompareTo(min) <= 0) return min;
        if (num.CompareTo(max) >= 0) return max;
        return num;
    }

    public static bool IsBetween<T>(T num, T min, T max) where T : IComparable<T>
    {
        return num.CompareTo(min) >= 0 && num.CompareTo(max) <= 0;
    }

    public static T BetweenOrDefault<T>(T num, T min, T max, T defaultValue = default) where T : IComparable<T>
    {
        if (num.CompareTo(min) >= 0 && num.CompareTo(max) <= 0) return num;
        return defaultValue;
    }

    public static float Remap(float value, float from1, float to1, float from2, float to2)
    {
        return ((value - from1) / (to1 - from1) * (to2 - from2)) + from2;
    }

    public static bool IsEvenNumber(int num)
    {
        return num % 2 == 0;
    }

    public static bool IsOddNumber(int num)
    {
        return num % 2 != 0;
    }

    public static float Lerp(float value1, float value2, float amount)
    {
        return value1 + ((value2 - value1) * amount);
    }

    public static Vector2 Lerp(Vector2 pos1, Vector2 pos2, float amount)
    {
        var x = Lerp(pos1.X, pos2.X, amount);
        var y = Lerp(pos1.Y, pos2.Y, amount);
        return new Vector2(x, y);
    }

    public static float RadianToDegree(float radian)
    {
        return radian * RadianPI;
    }

    public static float DegreeToRadian(float degree) => degree * DegreePI;

    public static Vector2 RadianToVector2(float radian)
    {
        return new Vector2((float)System.Math.Cos(radian), (float)System.Math.Sin(radian));
    }

    public static Vector2 RadianToVector2(float radian, float length) => RadianToVector2(radian) * length;

    public static Vector2 DegreeToVector2(float degree) => RadianToVector2(DegreeToRadian(degree));

    public static Vector2 DegreeToVector2(float degree, float length) =>
        RadianToVector2(DegreeToRadian(degree), length);

    public static float Vector2ToRadian(Vector2 direction) => (float)System.Math.Atan2(direction.Y, direction.X);

    public static float Vector2ToDegree(Vector2 direction) => RadianToDegree(Vector2ToRadian(direction));

    public static float LookAtRadian(Vector2 pos1, Vector2 pos2) =>
        (float)System.Math.Atan2(pos2.Y - pos1.Y, pos2.X - pos1.X);

    public static float LookAtRadian(PointF pos1, PointF pos2) =>
        (float)System.Math.Atan2(pos2.Y - pos1.Y, pos2.X - pos1.X);

    public static Vector2 LookAtVector2(Vector2 pos1, Vector2 pos2) => RadianToVector2(LookAtRadian(pos1, pos2));

    public static float LookAtDegree(Vector2 pos1, Vector2 pos2) => RadianToDegree(LookAtRadian(pos1, pos2));

    public static float LookAtDegree(PointF pos1, PointF pos2) => RadianToDegree(LookAtRadian(pos1, pos2));

    public static float Distance(Vector2 pos1, Vector2 pos2) =>
        (float)System.Math.Sqrt(System.Math.Pow(pos2.X - pos1.X, 2) + System.Math.Pow(pos2.Y - pos1.Y, 2));

    public static float Distance(PointF pos1, PointF pos2) =>
        (float)System.Math.Sqrt(System.Math.Pow(pos2.X - pos1.X, 2) + System.Math.Pow(pos2.Y - pos1.Y, 2));
}

