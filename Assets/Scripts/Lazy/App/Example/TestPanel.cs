using Lazy.Rx;
using UnityEngine;

namespace Lazy.App.Example
{
    /// <summary>
    /// ! ViewModel Layer
    /// </summary>
    public partial class TestPanel : MonoBehaviour, IViewModel
    {
        private void OnClickIncrease()
        {
            this.LazyCommand<IncreaseCommand, CountCommandArguments>(new CountCommandArguments() { Count = 2 });
        }

        private void OnClickDecrease()
        {
            this.LazyCommand<DecreaseCommand>();
        }

        private void OnClickOutput()
        {
            var val = _testSystem.FindMultiple();
            Debug.Log(val);
        }

        private ReadOnlyReactiveVariable<string> _count;

        private ITestModel _testModel;

        private ITestSystem _testSystem;

        private void Awake()
        {
            _testModel = this.GetModel<ITestModel>();
            _testSystem = this.GetSystem<ITestSystem>();
            _count = _testModel.Counter.ToReadOnlyReactiveProperty();
        }

        public IApp App => TestApp.Gate;
    }
}