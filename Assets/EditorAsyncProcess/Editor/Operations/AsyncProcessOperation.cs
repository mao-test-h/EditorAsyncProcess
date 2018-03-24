using System;


namespace EditorUtility
{
    /// <summary>
    /// プロセス待機用オペレーション
    /// </summary>
    public class WaitProcessOperation : IProcessOperation
    {
        // --------------------------------
        #region // Private Members

        /// <summary>
        /// 進行中の処理(完了したらfalseを返す)
        /// </summary>
        Func<bool> _progressProcess = null;

        /// <summary>
        /// 完了時コールバック
        /// </summary>
        Action _finishedCallback = null;

        #endregion // Private Members



        // ----------------------------------------------------------------
        #region // Public Methods

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="progressProcess">進行中の処理(完了したらfalseを返す)</param>
        /// <param name="finishedCallback">完了時コールバック</param>
        public WaitProcessOperation(Func<bool> progressProcess, Action finishedCallback)
        {
            this._progressProcess = progressProcess;
            this._finishedCallback = finishedCallback;
        }

        /// <summary>
        /// 毎フレーム更新
        /// </summary>
        /// <returns>進行中の処理が完了したらfalse</returns>
        public bool Update()
        {
            bool ret = this._progressProcess();
            if (!ret) { this._finishedCallback(); }
            return ret;
        }

        #endregion // Public Methods
    }
}