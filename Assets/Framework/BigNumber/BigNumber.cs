using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

/// <summary>
/// 超大实数类型，进行有限精度(10位)的超大实数的存储与四则运算
/// </summary>
public struct BigNumber {
    //10位有限精度
    public const int LIMIT_PRECISION = 10;

    /// <summary>
    /// 实数
    /// </summary>
    public double Number;

    /// <summary>
    /// 位数
    /// </summary>
    public int Digits;

    public BigNumber(double number, int digits) {
        var addDigits = 0;
        if (number >10) {
            addDigits = ToStandardNum(number, out var newNumber);
            number = newNumber;
        }
        Number = Math.Round(number, LIMIT_PRECISION); //取有限精度
        Digits = digits + addDigits;
    }

    public BigNumber(double number) {
        if (Math.Abs(number) < 1e-306) {
            this = Zero;
            return;
        }


        Digits = QuickToStandardNum(number, out Number);
        Number = Math.Round(Number, LIMIT_PRECISION); //取有限精度

        //UnityEngine.Debug.Log(this);
    }

    public BigNumber(string number) {
        if (number == "0" || number.StartsWith(".")) {
            this = Zero;
            return;
        }

        if (NumberUtils.isNumeric(number)) {
            var strings = number.Split('.');
            if (strings[0].Length <= 307) {
                Digits = QuickToStandardNum(double.Parse(number), out Number);
                Number = Math.Round(Number, LIMIT_PRECISION);
            }
            else {
                var substring = strings[0].Substring(0, 10);
                substring = substring.Insert(1, ".");
                Digits = strings[0].Length - 1;
                Number = double.Parse(substring);
            }
        }
        else if (NumberUtils.isScientific(number)) {
            var strings = number.Split('e');
            Number = double.Parse(strings[0]);
            Digits = int.Parse(strings[1]);
        }
        else {
            this = Zero;
            // Debug.Log($"{number} 转换失败");
        }
    }

    public static bool operator ==(BigNumber n1, BigNumber n2) {
        if (n1.Number == 0 && n2.Number == 0) {
            return true;
        }

        return n1.Number == n2.Number && n1.Digits == n2.Digits;
    }

    public static bool operator !=(BigNumber n1, BigNumber n2) {
        if (n1.Number == 0 && n2.Number == 0) {
            return false;
        }

        return n1.Number != n2.Number || n1.Digits != n2.Digits;
    }

    public static bool operator >(BigNumber n1, BigNumber n2) {
        if (n1 == Zero) {
            return n2.Number < 0;
        }

        //正数比较
        if (n1.Number > 0) {
            if (n2.Number <= 0)
                return true;
            if (n1.Digits > n2.Digits)
                return true;
            if (n1.Digits == n2.Digits) {
                return n1.Number > n2.Number;
            }

            return false;
        }

        //负数比较
        if (n2.Number >= 0)
            return false;
        if (n1.Digits < n2.Digits)
            return true;
        if (n1.Digits == n2.Digits) {
            return n1.Number > n2.Number;
        }

        return false;
    }

    public static bool operator <(BigNumber n1, BigNumber n2) {
        return n2 > n1 && n1 != n2;
    }

    public static bool operator >=(BigNumber n1, BigNumber n2) {
        return n1 == n2 || n1 > n2;
    }

    public static bool operator <=(BigNumber n1, BigNumber n2) {
        return n1 == n2 || n1 < n2;
    }

    /// <summary>
    /// 超大数加法运算，超过精度的数值不做计算
    /// Tips: (-1e308,-10] 与 [10,1e308)范围内，使用double运算
    /// </summary>
    /// <param name="n1"></param>
    /// <param name="n2"></param>
    /// <returns></returns>
    public static BigNumber operator +(BigNumber n1, BigNumber n2) {
        if (n1 == Zero)
            return n2;
        if (n2 == Zero)
            return n1;

        if (n1.Digits > 0 && n1.Digits <= 307 &&
            n2.Digits > 0 && n2.Digits <= 307) {
            if (n1.Digits == 307 && n2.Digits == 307)
            {
                var sum = n1.Number + n2.Number;
                if (sum >= minDoubleNumber * 10 && sum <= maxDoubleNumber * 10)
                {
                    return new BigNumber(n1.ToDouble() + n2.ToDouble());
                }
            }
            else
            {
                return new BigNumber(n1.ToDouble() + n2.ToDouble());
            }
        }

        if (n1.Number > 0 && n2.Number > 0
            || n1.Number < 2 && n2.Number < 0) {
            //同号加法运算
            if (Math.Abs(n1.Digits - n2.Digits) > LIMIT_PRECISION) {
                return n1.Digits > n2.Digits ? n1 : n2;
            }
            else {
                BigNumber sum = new BigNumber();

                BigNumber bigger = n1.Digits >= n2.Digits ? n1 : n2; //较大位数的数
                BigNumber smaller = n1.Digits < n2.Digits ? n1 : n2; //较小位数的数

                int subDigits = bigger.Digits - smaller.Digits;

                double power = Math.Pow(10, subDigits);
                double biggerNum = bigger.Number * power;
                //UnityEngine.Debug.Log("Bigger Num = " + biggerNum);
                double sumNum = biggerNum + smaller.Number;
                //sumNum = Math.Round(sumNum, 0);//运算结果只保留有限位数,并取整

                //UnityEngine.Debug.Log("sunNum = " + sumNum);

                double standSum = sumNum;
                int add_digit = ToStandardNum(sumNum, out standSum);

                //UnityEngine.Debug.Log("standSum = " + standSum + ", add_digit = " + add_digit);

                sum.Number = Math.Round(standSum, LIMIT_PRECISION);
                sum.Digits = smaller.Digits + add_digit;

                return sum;
            }
        }
        else {
            //异号运算，转化为减法运算
            if (n1.Number > 0)
                return n1 - (-n2);
            else
                return n2 - (-n1);
        }
    }

    /// <summary>
    /// 超大数减法运算，超过精度的数值不做计算
    /// Tips: (-1e308,-10] 与 [10,1e308)范围内，使用double运算
    /// </summary>
    /// <param name="n1"></param>
    /// <param name="n2"></param>
    /// <returns></returns>
    public static BigNumber operator -(BigNumber n1, BigNumber n2) {
        if (n1 == Zero)
            return -n2;
        if (n2 == Zero)
            return n1;

        if (n1.Digits > 0 && n1.Digits <= 307 &&
            n2.Digits > 0 && n2.Digits <= 307) {
            if (n1.Digits == 307 && n2.Digits == 307)
            {
                var sum = n1.Number - n2.Number;
                if (sum >= minDoubleNumber * 10 && sum <= maxDoubleNumber * 10)
                {
                    return new BigNumber(n1.ToDouble() - n2.ToDouble());
                }
            }
            else
            {
                return new BigNumber(n1.ToDouble() - n2.ToDouble());
            }
        }

        if (n1.Number > 0) {
            if (n2.Number > 0) { //n2>0
                if (Math.Abs(n1.Digits - n2.Digits) > LIMIT_PRECISION) { //位数差距太大，舍去不计算
                    return n1.Digits > n2.Digits ? n1 : -n2;
                }
                else { //实际减法运算,同号为正形
                    BigNumber sub = Zero;
                    if (n1 > n2) {
                        int subDigits = n1.Digits - n2.Digits;
                        double power = Math.Pow(10, subDigits);
                        double biggerNum = n1.Number * power;
                        double subNum = biggerNum - n2.Number;

                        //UnityEngine.Debug.Log("subNum = " + subNum);

                        double standSum = subNum;
                        int add_digit = ToStandardNum(subNum, out standSum);

                        sub.Number = Math.Round(standSum, LIMIT_PRECISION);
                        sub.Digits = n2.Digits + add_digit;
                    }
                    else if (n1 < n2) {
                        int subDigits = n2.Digits - n1.Digits;
                        double power = Math.Pow(10, subDigits);

                        double biggerNum = n2.Number * power;
                        double subNum = n1.Number - biggerNum;

                        double standSum = subNum;
                        int add_digit = ToStandardNum(subNum, out standSum);

                        sub.Number = Math.Round(standSum, LIMIT_PRECISION);
                        sub.Digits = n1.Digits + add_digit;
                    }
                    else {
                        //n1=n2,return zero;
                    }

                    return sub;
                }
            }
            else { //n2<0,异号运算,转换为加法
                return n1 + (-n2);
            }
        }
        else { //n1<0
            if (n2.Number > 0) { //n2>0,异号运算，转换为同号加法
                return n1 + (-n2);
            }
            else { //n2<0,同号运算，转换为异号加法
                return n1 + (-n2);
            }
        }
    }

    public static BigNumber operator -(BigNumber n) {
        n.Number = -n.Number;
        return n;
    }


    /// <summary>
    /// 超大数乘法运算,保留有限位数
    /// </summary>
    /// <param name="n1"></param>
    /// <param name="n2"></param>
    /// <returns></returns>
    public static BigNumber operator *(BigNumber n1, BigNumber n2) {
        if (n1 == Zero || n2 == Zero)
            return Zero;

        BigNumber res = new BigNumber();
        res.Number = Math.Round(n1.Number * n2.Number, LIMIT_PRECISION); //保留有限位数
        double standNum = res.Number;
        int add_digits = ToStandardNum(res.Number, out standNum);
        res.Number = standNum;
        res.Digits = n1.Digits + n2.Digits + add_digits;
        return res;
    }

    /// <summary>
    /// 超大数除法运算,保留有限位数
    /// </summary>
    /// <param name="n1"></param>
    /// <param name="n2"></param>
    /// <returns></returns>
    public static BigNumber operator /(BigNumber n1, BigNumber n2) {
        if (n2 == Zero) { //抛出除0异常
            throw new DivideByZeroException();
        }

        var res = new BigNumber {Number = Math.Round(n1.Number / n2.Number, LIMIT_PRECISION)};
        //保留有限位数
        double standNum = res.Number;
        int add_digits = ToStandardNum(res.Number, out standNum);
        res.Number = standNum;
        res.Digits = n1.Digits - n2.Digits + add_digits;
        return res;
    }

    public static BigNumber operator *(BigNumber left, float right) {
        return left * new BigNumber(right);
    }

    public static BigNumber operator *(BigNumber left, double right) {
        return left * new BigNumber(right);
    }


    public static BigNumber Parse(string str) {
        return new BigNumber(str);
    }

    /// <summary>
    /// 将数字转换为科学记数法表示
    /// </summary>
    /// <param name="oldNumber">旧数字</param>
    /// <param name="newNumber">新数字的实部</param>
    /// <returns>新数字的位数</returns>
    public static int ToStandardNum(double oldNumber, out double newNumber) {
        int digits = 0;
        int sign = oldNumber >= 0 ? 1 : -1;
        newNumber = Math.Abs(oldNumber);

        if (newNumber >= 10) {
            while (newNumber >= 10) {
                newNumber = newNumber / 10;
                digits++;
            }
        }
        else if (newNumber < 1 && newNumber > 0) {
            while (newNumber < 1 && newNumber > 0) {
                newNumber = newNumber * 10;
                digits--;
            }
        }

        newNumber = newNumber * sign;
        return digits;
    }

    /// <summary>
    /// 将数字转换为科学记数法表示(快速算法)，数值需小于 1e+308
    /// </summary>
    /// <param name="oldNumber"></param>
    /// <param name="newNumber"></param>
    /// <returns></returns>
    public static int QuickToStandardNum(double oldNumber, out double newNumber) {
        int sign = oldNumber >= 0 ? 1 : -1;
        if (Math.Abs(oldNumber) >= 1) {
            newNumber = Math.Abs(oldNumber);

            int end1 = 0;
            int end2 = 308;

            QuickGetDigitsRange(newNumber, ref end1, ref end2);

            double endNum = newNumber;
            int digits = 0;

            //UnityEngine.Debug.Log("end1 = " + end1 + ",end2 = " + end2);

            if (newNumber / Math.Pow(10, end1) < 10) {
                digits = end1;
                endNum = newNumber / Math.Pow(10, end1);
            }
            else if (newNumber / Math.Pow(10, end2) < 10) {
                digits = end2;
                endNum = newNumber / Math.Pow(10, end2);
            }

            newNumber = sign * endNum;

            //UnityEngine.Debug.Log("number " + newNumber + ",digis = " + digits);
            return digits;
        }
        else {
            newNumber = Math.Abs(oldNumber);

            int end1 = -308;
            int end2 = -1;

            QuickGetDigitsRange(newNumber, ref end1, ref end2);

            double endNum = newNumber;
            int digits = 0;

            //UnityEngine.Debug.Log("end1 = " + end1 + ",end2 = " + end2 + " new = " + newNumber);

            if (newNumber / Math.Pow(10, end1) < 10) {
                digits = end1;
                endNum = newNumber * Math.Pow(10, -end1);
            }
            else if (newNumber / Math.Pow(10, end2) < 10) {
                digits = end2;
                endNum = newNumber * Math.Pow(10, -end2);
            }

            newNumber = sign * endNum;

            //UnityEngine.Debug.Log("number " + newNumber + ",digis = " + digits);
            return digits;
        }
    }

    /// <summary>
    /// 使用二分法快速确定一个 double数值 转换为科学记数法的位数范围
    /// </summary>
    /// <param name="number"></param>
    /// <param name="digits_min"></param>
    /// <param name="digits_max"></param>
    public static void QuickGetDigitsRange(double number, ref int digits_min, ref int digits_max) {
        //UnityEngine.Debug.Log("QuickGetDigitsRange");

        if (digits_max - digits_min <= 1) {
            return;
        }

        int mid = (digits_min + digits_max) / 2;

        double pow_m = Math.Pow(10, mid);

        var testNumber = number / pow_m;
        if (testNumber >= 10) {
            digits_min = mid;
            QuickGetDigitsRange(number, ref digits_min, ref digits_max);
        }
        else {
            digits_max = mid;
            QuickGetDigitsRange(number, ref digits_min, ref digits_max);
        }
    }

    /// <summary>
    /// 转化为Double类型，若超出范围，则返回Double类型的最大值
    /// </summary>
    /// <returns></returns>
    public double ToDouble() {
        if (Digits < 308) {
            return Number * Math.Pow(10, Digits);
        }
        else if (Digits == 308 && Number <= maxDoubleNumber && Number >= minDoubleNumber) {
            return Number * Math.Pow(10, Digits);
        }

        //UnityEngine.Debug.Log("越界");
        return Number > 0 ? double.MaxValue : double.MinValue;
    }

    public static implicit operator BigNumber(int value) => new BigNumber(value);
    public static implicit operator BigNumber(double value) => new BigNumber(value);
    public static implicit operator BigNumber(float value) => new BigNumber(value);


    // public static BigNumber Pow(double number, double scale) {
    //     const int maxScale = 307;
    //     if (scale <= maxScale) return new BigNumber(Math.Pow(number, scale));
    //     var other = scale % maxScale;
    //     return new BigNumber(Math.Pow(number, 307)) * Pow(number, other);
    // }

    public static BigNumber Pow(BigNumber x, int y) {
        if (y < 0)
            throw new Exception("power Count can't be less than Zero!");

        if (y == 0)
            return One;
        else if (y == 1)
            return x;

        BigNumber result = One;
        var root_pow_counts = new List<int>();
        DevideIntByBinary(y, ref root_pow_counts);

        foreach (var count in root_pow_counts) {
            var s_result = PowRoot(x, count + 1);
            // UnityEngine.Debug.Log("pow root count = " + count + " => " + Math.Pow(2, count) + "     s_result = " + s_result.ToDouble());
            result = result * s_result;
        }

        return result;
    }

    static BigNumber PowRoot(BigNumber x, int y) {
        var root = x;
        var result = root;
        for (int i = 0; i < y; i++) {
            //UnityEngine.Debug.Log(result.ToDouble() + "   root = " + root.ToDouble());
            result = i == 0 ? root : result * result;
        }


        return result;
    }

    /// <summary>
    /// 将数值用2进制进行分解
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public static void DevideIntByBinary(int x, ref List<int> result) {
        //UnityEngine.Debug.Log(Math.Pow(2, 256));
        double root = 1;
        double _last_root = root;

        int count = 0;

        for (int i = 0; i <= 256; i++) {
            root = i == 0 ? 1 : root * 2;
            //UnityEngine.Debug.Log(root);
            if (root > x) {
                break;
            }

            _last_root = root;
            count = i;
        }

        result.Add(count);
        var rest_x = x - (int) _last_root;
        if (rest_x == 0) {
            return;
        }
        else {
            DevideIntByBinary(rest_x, ref result);
        }
    }


    #region 重载

    public override string ToString() {
        if (this == Zero)
            return "0";

        return string.Format("{0}e{1}", Number, Digits);
    }

    public override bool Equals(object obj) {
        return base.Equals(obj);
    }

    public override int GetHashCode() {
        return base.GetHashCode();
    }

    #endregion

    #region 常量

    /// <summary>
    /// 常数0
    /// </summary>
    public static BigNumber Zero {
        get { return new BigNumber(0, 0); }
    }

    public static BigNumber One {
        get { return new BigNumber(1, 0); }
    }

    const double minDoubleNumber = double.MinValue / 1e+308;
    const double maxDoubleNumber = double.MaxValue / 1e+308;

    #endregion

#if UNITY_EDITOR

    // [UnityEditor.MenuItem("Test/Big Number Test")]
    public static void Test() {
        //int end1 = 0;
        //int end2 = 307;
        //QuickGetDigitsRange(12651, ref end1, ref end2);

        //UnityEngine.Debug.Log("end1 = " + end1 + ",end2 = " + end2);
        //var b = new BigNumber(1.111222333444555666e123);

        //BigNumber b1 = new BigNumber(9.6, 0);
        //BigNumber b2 = new BigNumber(8.7, 0);
        //UnityEngine.Debug.Log((b1 - b2).ToDouble());
        //UnityEngine.Debug.Log((b1 + b2).ToDouble());

        //UnityEngine.Debug.Log(new BigNumber(double.MaxValue));
        //UnityEngine.Debug.Log(new BigNumber(double.MinValue));

        // BigNumber bigNumber = new BigNumber("4.3e-101");
        // UnityEngine.Debug.Log(bigNumber);

        
    }
#endif
}