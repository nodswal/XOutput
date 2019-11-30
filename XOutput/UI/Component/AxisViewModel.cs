﻿using XOutput.Devices;
using XOutput.Tools;

namespace XOutput.UI.Component
{
    public class AxisViewModel : ViewModelBase<AxisModel>
    {
        private const int max = 100;

        [ResolverMethod(Scope.Prototype)]
        public AxisViewModel(AxisModel model) : base(model)
        {
            Model.Max = max;
        }

        public void Initialize(InputSource type)
        {
            Model.Type = type;
        }

        public void UpdateValues()
        {
            Model.Value = (int)(Model.Type.Value * Model.Max);
        }
    }
}
