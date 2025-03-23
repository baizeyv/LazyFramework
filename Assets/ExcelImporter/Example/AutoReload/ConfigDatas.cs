

using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using IEnumerator = System.Collections.IEnumerator;

namespace AutoReloadDemo
{

    public class EnemyExcelData 
    {
        public int ID;
        public string Name;
        public int HP;
        public int Attack;
        public float ExperienceReward;
        public string InitWeaponName;
        public float MoveSpeed;
        public long PointReward;

        public override string ToString()
        {
            return $"ID:{ID} Name:{Name} HP:{HP} Attack:{Attack} ExperienceReward:{ExperienceReward} InitWeaponName:{InitWeaponName} MoveSpeed:{MoveSpeed} PointReward:{PointReward}";
        }
    }

    public class WeaponExcelData{
        public int ID;
        public string Name;
        public float MinDamageMin;
        public float MinDamageMax;
        public float MaxDamageMin;
        public float MaxDamageMax;
        public float AttackSpeedMin;
        public float AttackSpeedMax;
        public float AppearRateWeight;
        public string DamageType;

        public override string ToString()
        {
            return $"ID:{ID} Name:{Name} MinDamageMin:{MinDamageMin} MinDamageMax:{MinDamageMax} MaxDamageMin:{MaxDamageMin} MaxDamageMax:{MaxDamageMax} AttackSpeedMin:{AttackSpeedMin} AttackSpeedMax:{AttackSpeedMax} AppearRateWeight:{AppearRateWeight} DamageType:{DamageType}";
        }
    }
    public class ConfigDatas : Singleton<ConfigDatas>
    {
        public Dictionary<string, EnemyExcelData> EnemyExcelDataDic { get => excelLoaderOfEnemyTable.ExcelDataDic; }
        public Dictionary<string, WeaponExcelData> WeaponExcelDataDic { get => excelLoaderOfWeaponTable.ExcelDataDic; }


        [SerializeField]
        private string pathOfEnemyTable = "/Datas/怪物表.xlsx";

        [SerializeField]
        private string pathOfWeaponTable = "/Datas/武器表.xlsx";


        [SerializeField]
        private float reloadSecondFrequency = 60;
        [SerializeField]
        private bool showDebugLog = true;

        WaitForSeconds waitForSecondsCache;

        ExcelLoader<EnemyExcelData> excelLoaderOfEnemyTable;
        ExcelLoader<WeaponExcelData> excelLoaderOfWeaponTable;

        List<Reloadable> reloadables = new List<Reloadable>();

        protected override void Awake()
        {
            base.Awake();
            excelLoaderOfEnemyTable = new ExcelLoader<EnemyExcelData>(Application.streamingAssetsPath+ pathOfEnemyTable, showDebugLog: showDebugLog);
            excelLoaderOfWeaponTable = new ExcelLoader<WeaponExcelData>(Application.streamingAssetsPath+ pathOfWeaponTable, showDebugLog: showDebugLog);
            reloadables.Add(excelLoaderOfEnemyTable);
            reloadables.Add(excelLoaderOfWeaponTable);
        }

        IEnumerator Start()
        {
            waitForSecondsCache = new WaitForSeconds(reloadSecondFrequency);
            while (true)
            {
                yield return waitForSecondsCache;
                foreach (var item in reloadables)
                {
                    item.ReloadAsync();
                }
            }
        }
    }
}