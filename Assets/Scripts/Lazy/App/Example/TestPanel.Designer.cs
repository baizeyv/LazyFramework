using System;
using Lazy.Utility;

namespace Lazy.App.Example
{
    /// <summary>
    /// ! Data Binding Intermediary
    /// </summary>
    public partial class TestPanel
    {
        private IDisposable _countDisposable;

        private void OnEnable()
        {
            _count.SubscribeToText(testText);
            increaseButton.onClick.AddListener(OnClickIncrease);
            decreaseButton.onClick.AddListener(OnClickDecrease);
            outputButton.onClick.AddListener(OnClickOutput);
        }

        private void OnDisable()
        {
            _countDisposable?.Dispose();
            increaseButton.onClick.RemoveListener(OnClickIncrease);
            decreaseButton.onClick.RemoveListener(OnClickDecrease);
            outputButton.onClick.RemoveListener(OnClickOutput);
        }
    }
}