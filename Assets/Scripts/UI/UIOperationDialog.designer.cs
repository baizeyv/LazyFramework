using System;
using UnityEngine;
using UnityEngine.UI;
using Lazy;

namespace Lazy.Melody
{
    public partial class UIOperationDialog : UIDialog
    {
        #region Generation
        public const string Name = "UIOperationDialog";

        private readonly UIOperationDialogData _defaultData = new();

        protected UIOperationDialogData Data => PanelData as UIOperationDialogData ?? _defaultData;
        #endregion

        #region Field

		[SerializeField]
		public TMPro.TMP_InputField mVitaStepLimit;
		[SerializeField]
		public TMPro.TextMeshProUGUI mVitaInputJson;
		[SerializeField]
		public UnityEngine.UI.Button mSelectVitaInputBtn;
		[SerializeField]
		public TMPro.TextMeshProUGUI mVitaOutputCsv;
		[SerializeField]
		public UnityEngine.UI.Button mSelectVitaOutputBtn;
		[SerializeField]
		public UnityEngine.UI.Button mExportVitaBtn;
		[SerializeField]
		public UnityEngine.UI.Button mStopExportVitaBtn;
		[SerializeField]
		public TMPro.TextMeshProUGUI mQueryCalcLbl;
		[SerializeField]
		public UnityEngine.UI.Button mQueryVitaExportCalcBtn;
		[SerializeField]
		public TMPro.TMP_InputField mPVStepLimit;
		[SerializeField]
		public TMPro.TextMeshProUGUI mPVInputTxt;
		[SerializeField]
		public UnityEngine.UI.Button mSelectPVInputBtn;
		[SerializeField]
		public TMPro.TextMeshProUGUI mPVOutputCsv;
		[SerializeField]
		public UnityEngine.UI.Button mSelectPVOutputBtn;
		[SerializeField]
		public UnityEngine.UI.Toggle mSuit1Toggle;
		[SerializeField]
		public UnityEngine.UI.Toggle mSuit2Toggle;
		[SerializeField]
		public UnityEngine.UI.Toggle mSuit3Toggle;
		[SerializeField]
		public UnityEngine.UI.Toggle mSuit4Toggle;
		[SerializeField]
		public UnityEngine.UI.Button mExportPVBtn;
		[SerializeField]
		public UnityEngine.UI.Button mStopExportPVBtn;
		[SerializeField]
		public TMPro.TextMeshProUGUI mQueryPVSuit1CalcLbl;
		[SerializeField]
		public TMPro.TextMeshProUGUI mQueryPVSuit2CalcLbl;
		[SerializeField]
		public TMPro.TextMeshProUGUI mQueryPVSuit3CalcLbl;
		[SerializeField]
		public TMPro.TextMeshProUGUI mQueryPVSuit4CalcLbl;
		[SerializeField]
		public UnityEngine.UI.Button mQueryPVExportSuit1CalcBtn;
		[SerializeField]
		public UnityEngine.UI.Button mQueryPVExportSuit2CalcBtn;
		[SerializeField]
		public UnityEngine.UI.Button mQueryPVExportSuit3CalcBtn;
		[SerializeField]
		public UnityEngine.UI.Button mQueryPVExportSuit4CalcBtn;

        #endregion

        protected override void ClearUIComponents()
        {
			mVitaStepLimit = null;
			mVitaInputJson = null;
			mSelectVitaInputBtn = null;
			mVitaOutputCsv = null;
			mSelectVitaOutputBtn = null;
			mExportVitaBtn = null;
			mStopExportVitaBtn = null;
			mQueryCalcLbl = null;
			mQueryVitaExportCalcBtn = null;
			mPVStepLimit = null;
			mPVInputTxt = null;
			mSelectPVInputBtn = null;
			mPVOutputCsv = null;
			mSelectPVOutputBtn = null;
			mSuit1Toggle = null;
			mSuit2Toggle = null;
			mSuit3Toggle = null;
			mSuit4Toggle = null;
			mExportPVBtn = null;
			mStopExportPVBtn = null;
			mQueryPVSuit1CalcLbl = null;
			mQueryPVSuit2CalcLbl = null;
			mQueryPVSuit3CalcLbl = null;
			mQueryPVSuit4CalcLbl = null;
			mQueryPVExportSuit1CalcBtn = null;
			mQueryPVExportSuit2CalcBtn = null;
			mQueryPVExportSuit3CalcBtn = null;
			mQueryPVExportSuit4CalcBtn = null;

        }
    }
}
// Generation Time: Friday, April 25, 2025 2:04:14 PM
// Generation ID: 2626cdb5-5fb1-468c-9799-bd3b72480fbd
