using Lazy.Utility;

namespace Lazy.App.Example
{
    public struct CountCommandArguments
    {
        public int Count;
    }

    public class IncreaseCommand : ABSCommand<CountCommandArguments>
    {
        private ITestModel _model;

        protected override void OnFire(CountCommandArguments arg)
        {
            _model ??= this.GetModel<ITestModel>();
            if (_model.Counter.Value.TryParseTo(out int value))
            {
                _model.Counter.Value = (value + arg.Count).ToString();
            }
            else
            {
                _model.Counter.Value = arg.Count.ToString();
            }
        }
    }

    public class DecreaseCommand : ABSCommand
    {
        private ITestModel _model;

        protected override void OnFire()
        {
            _model ??= this.GetModel<ITestModel>();
            if (_model.Counter.Value.TryParseTo(out int value))
            {
                _model.Counter.Value = (value - 1).ToString();
            }
            else
            {
                _model.Counter.Value = "1";
            }
        }
    }
}