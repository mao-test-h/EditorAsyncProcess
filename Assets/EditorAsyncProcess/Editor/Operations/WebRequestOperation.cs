//#define WEB_REQUEST_OPERATION_ENABLE_API_LOG // 定義時はAPI通信ログを出力

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEditor;


namespace EditorUtility
{
    /// <summary>
    /// エラータイプ
    /// </summary>
    public enum ErrorType
    {
        None = 0,
        TimeOut,
        Other,
    }

    /// <summary>
    /// 通信処理用オペレーション
    /// </summary>
    public class WebRequestOperation : IProcessOperation
    {
        // --------------------------------
        #region // Constants

        /// <summary>
        /// 最大リトライ回数
        /// </summary>
        const int RetryCount = 5;

        /// <summary>
        /// リトライ前の待機時間(sec)
        /// </summary>
        const float RetryWaitTime = 60f;

        /// <summary>
        /// タイムアウト(sec)
        /// </summary>
        const float TimeOut = 60f;

        #endregion // Constants

        // --------------------------------
        #region // Inner Classes

        /// <summary>
        /// レスポンス情報
        /// </summary>
        public class Response
        {
            /// <summary>
            /// 取得データ
            /// </summary>
            public string ResponseData { get; private set; }

            /// <summary>
            /// システムエラー情報
            /// </summary>
            public string Error { get; private set; }

            /// <summary>
            /// コンストラクタ
            /// </summary>
            public Response(string responseData, string error)
            {
                this.ResponseData = responseData;
                this.Error = error;
            }
        }

        #endregion // Inner Classes

        // --------------------------------
        #region // Private Members

        /// <summary>
        /// URL
        /// </summary>
        string _url;

        /// <summary>
        /// ヘッダー
        /// </summary>
        Dictionary<string, string> _headers = null;

        /// <summary>
        /// リクエスト時のbody(GET時はnull)
        /// </summary>
        WWWForm _body = null;

        /// <summary>
        /// 完了時コールバック
        /// </summary>
        Action<Response> _finishedCallback = null;

        /// <summary>
        /// リクエストを送ったタイミング
        /// </summary>
        float _sendRequestTime = -1f;

        /// <summary>
        /// Errorフラグ
        /// </summary>
        ErrorType _errorType = ErrorType.None;

        /// <summary>
        /// 現在のリトライ回数
        /// </summary>
        int _currentRetryCount = 0;

        /// <summary>
        /// リトライ前のスリープ時間計測用
        /// </summary>
        float _retryWaitTimeCount = -1f;

        /// <summary>
        /// リクエスト
        /// </summary>
        UnityWebRequest _request = null;

        /// <summary>
        /// 非同期処理用オペレーション
        /// </summary>
        UnityWebRequestAsyncOperation _operation = null;

        #endregion // Private Members

        // --------------------------------
        #region // Properties

        /// <summary>
        /// 進捗状況の取得
        /// </summary>
        public float GetProgress { get { return this._operation.progress; } }

        #endregion // Properties



        // ----------------------------------------------------------------
        #region // Public Methods

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="url">URL</param>
        /// <param name="finishedCallback">完了時コールバック</param>
        /// <param name="headers">ヘッダー</param>
        /// <param name="body">ボディ(GET通信時であればnullを入れる事)</param>
        public WebRequestOperation(
            string url,
            Action<Response> finishedCallback,
            Dictionary<string, string> headers,
            WWWForm body = null)
        {
            this._url = url;
            this._headers = headers;
            this._body = body;
            this._request = this.CreateRequest(url, body);
            this._finishedCallback = finishedCallback;
            this._errorType = ErrorType.None;
            this._retryWaitTimeCount = this._sendRequestTime = -1f;
        }

        /// <summary>
        /// 毎フレーム更新
        /// </summary>
        /// <returns>進行中の処理が完了したらfalse</returns>
        public bool Update()
        {
            // 初期値が入っていたらリクエストを飛ばす
            if (this._sendRequestTime <= -1f)
            {
                this.SendRequest();
            }

            float realtimeSinceStartup = Time.realtimeSinceStartup;

            // リトライ処理
            float retryWaitTimeCount = this._retryWaitTimeCount;
            if (retryWaitTimeCount > 0f)
            {
                // リトライ開始前の待機
                if ((realtimeSinceStartup - retryWaitTimeCount) >= WebRequestOperation.RetryWaitTime)
                {
                    return true;
                }
                this._retryWaitTimeCount = -1f;
                this.SendRequest();
            }

            // 通信完了待機
            if (!this._request.isDone && (this._errorType == ErrorType.None))
            {
                if ((realtimeSinceStartup - this._sendRequestTime) >= WebRequestOperation.TimeOut)
                {
                    // タイムアウト判定
                    this._errorType = ErrorType.TimeOut;
                }
                else
                {
                    return true;
                }
            }

            // エラー判定
            if ((this._errorType == ErrorType.None) && (this._request.isNetworkError || this._request.responseCode != 200))
            {
                // HACK. 内容を見てもっと細分化した方が良い
                this._errorType = ErrorType.Other;
            }

            string responseText = null;
            if (this._errorType != ErrorType.None)
            {
                // 通信失敗 + リトライ
#if WEB_REQUEST_OPERATION_ENABLE_API_LOG
                Debug.LogWarningFormat(
                    "Error.... [url : {0}], [code : {1}], [type : {2}], [message : {3}], [count : {4}]",
                    this._request.url, this._request.responseCode, this._errorType, this._request.error, this._currentRetryCount);
#endif
                if (this.Retry())
                {
                    return true;
                }
            }
            else
            {
                // 通信成功
                responseText = DownloadHandlerBuffer.GetContent(this._request);
#if WEB_REQUEST_OPERATION_ENABLE_API_LOG
                Debug.LogFormat(
                    "Success... [url : {0}], [code : {1}], [response : {2}]",
                    this._request.url, this._request.responseCode, responseText);
#endif
            }

            this._finishedCallback(new Response(responseText, this._request.error));
            this._request.Dispose();
            return false;
        }

        #endregion // Public Methods

        // ----------------------------------------------------------------
        #region // Private Methods

        /// <summary>
        /// UnityWebRequestの生成
        /// </summary>
        /// <param name="url">URL</param>
        /// <param name="body">ボディ(GET通信時であればnullを入れる事)</param>
        /// <returns>生成したUnityWebRequest</returns>
        UnityWebRequest CreateRequest(string url, WWWForm body = null)
        {
            return (body == null) ? UnityWebRequest.Get(url) : UnityWebRequest.Post(url, body);
        }

        /// <summary>
        /// リトライ処理
        /// </summary>
        /// <returns>最大リトライ回数を超えていればfalse</returns>
        bool Retry()
        {
            ++this._currentRetryCount;
            if (this._currentRetryCount > WebRequestOperation.RetryCount) { return false; }
            this._request.Dispose();
            this._request = this.CreateRequest(this._url, this._body);
            this._retryWaitTimeCount = Time.realtimeSinceStartup;
            return true;
        }

        /// <summary>
        /// 通信処理開始
        /// </summary>
        void SendRequest()
        {
            this._errorType = ErrorType.None;
            this._sendRequestTime = Time.realtimeSinceStartup;
            if (this._headers != null)
            {
                foreach (var header in this._headers)
                {
                    this._request.SetRequestHeader(header.Key, header.Value);
                }
            }
            this._operation = this._request.SendWebRequest();
        }

        #endregion // Private Methods
    }
}