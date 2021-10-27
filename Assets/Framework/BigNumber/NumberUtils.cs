public class NumberUtils {
    /// <summary>
    /// 判断字符串是不是数字：不能有两个小数点、负号只能在最前面、除了小数点和负号，只能是数字。
    /// </summary>        
    public static bool isNumeric(string strInput) {
        var ca = strInput.ToCharArray();
        var pointcut = 0;
        for (var i = 0; i < ca.Length; i++) {
            if ((ca[i] < '0' || ca[i] > '9') && ca[i] != '.' && ca[i] != '-') return false;
            if (ca[i] == '-' && i != 0) return false;

            if (ca[i] == '.') pointcut++;
        }

        return pointcut <= 1;
    }

    /// <summary>
    /// 判断字符传是不是科学计数法
    /// </summary>
    /// <param name="strInput"></param>
    /// <returns></returns>
    public static bool isScientific(string strInput) {
        var ca = strInput.ToCharArray();
        var pointcut = 0;
        var ecut = 0;
        for (var i = 0; i < ca.Length; i++) {
            if ((ca[i] < '0' || ca[i] > '9') && ca[i] != '.' && ca[i] != '-' && ca[i] != 'e') return false;
            if (ca[i] == '-' && i != 0) {
                if (ca[i - 1] != 'e') {
                    return false;
                }
            }
            if (ca[i] == '.') pointcut++;
            if (ca[i] == 'e') ecut++;
        }

        return pointcut <= 1 && ecut <= 1;
    }
}