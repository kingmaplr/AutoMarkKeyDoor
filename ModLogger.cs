using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace AutoMarkKeyDoor
{
    /// <summary>
    /// Mod日志工具类
    /// </summary>
    public static class ModLogger
    {
        private const string LogPrefix = "[AutoMarkKeyDoor] ";
        
        #region 调试日志 (仅 DEBUG 模式)
        
        /// <summary>
        /// 输出调试信息 (仅在 DEBUG 模式下生效)
        /// </summary>
        [Conditional("DEBUG")]
        public static void Log(string message)
        {
            Debug.Log(LogPrefix + message);
        }
        
        /// <summary>
        /// 输出带分类的调试信息 (仅在 DEBUG 模式下生效)
        /// </summary>
        [Conditional("DEBUG")]
        public static void Log(string category, string message)
        {
            Debug.Log($"{LogPrefix}[{category}] {message}");
        }
        
        /// <summary>
        /// 输出调试警告 (仅在 DEBUG 模式下生效)
        /// </summary>
        [Conditional("DEBUG")]
        public static void LogWarning(string message)
        {
            Debug.LogWarning(LogPrefix + message);
        }
        
        /// <summary>
        /// 输出带分类的调试警告 (仅在 DEBUG 模式下生效)
        /// </summary>
        [Conditional("DEBUG")]
        public static void LogWarning(string category, string message)
        {
            Debug.LogWarning($"{LogPrefix}[{category}] {message}");
        }
        
        #endregion
        
        #region 错误日志 (始终输出)
        
        /// <summary>
        /// 输出错误信息
        /// </summary>
        public static void LogError(string message)
        {
            Debug.LogError(LogPrefix + message);
        }
        
        /// <summary>
        /// 输出带分类的错误信息
        /// </summary>
        public static void LogError(string category, string message)
        {
            Debug.LogError($"{LogPrefix}[{category}] {message}");
        }
        
        #endregion
        
        #region 详细日志 (仅 VERBOSE 模式)
        
        /// <summary>
        /// 输出详细日志 (仅在定义 VERBOSE 宏时生效)
        /// </summary>
        [Conditional("VERBOSE")]
        public static void LogVerbose(string message)
        {
            Debug.Log(LogPrefix + "[Verbose] " + message);
        }
        
        /// <summary>
        /// 输出带分类的详细日志 (仅在定义 VERBOSE 宏时生效)
        /// </summary>
        [Conditional("VERBOSE")]
        public static void LogVerbose(string category, string message)
        {
            Debug.Log($"{LogPrefix}[{category}][Verbose] {message}");
        }
        
        #endregion
    }
}
