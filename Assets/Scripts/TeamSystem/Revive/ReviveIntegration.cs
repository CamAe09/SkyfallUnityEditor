using Fusion;
using UnityEngine;

namespace TPSBR
{
    public class ReviveIntegration : ContextBehaviour
    {
        [SerializeField]
        private bool _enableReviveSystem = true;

        public void SetReviveSystemEnabled(bool enabled)
        {
            _enableReviveSystem = enabled;
        }
    }
}
