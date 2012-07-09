using System;
using MData.Core.Base;

namespace MData.SandBox
{
    public class AuditableLogic : LogicBase<IAuditable>
    {
        protected override void Init()
        {
            base.Init();

            CurrentInstance.CreatedOn = DateTime.Now;
            CurrentInstance.ModifiedOn = CurrentInstance.CreatedOn;
        }
    }
}