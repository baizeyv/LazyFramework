using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lazy;
using UnityEditor;
using UnityEngine;

namespace LazyEditor
{
    /// <summary>
    /// * 查找绑定的工具
    /// </summary>
    public class BindSearchHelper
    {
        public static void Search(GenCodeTask task, Dictionary<string, int> propertyNameMap)
        {
            // # 找到当前对象的子元素中的所有 IBindGroup 的 transform
            var bindGroupTransforms = task
                .gameObject.GetComponentsInChildren<IBindGroup>(true)
                .Select(x => (x as Component)?.transform)
                .Where(x => x != null && x != task.gameObject.transform);

            // # 找到当前对象的子元素中的所有 IBind
            var binds = task
                .gameObject.GetComponentsInChildren<IBind>(true)
                .Where(x => x.Transform != task.gameObject.transform);

            foreach (var bind in binds)
                if (
                    !bindGroupTransforms.Any(x =>
                        bind.Transform.IsChildOf(x) && bind.Transform != x
                    )
                )
                {
                    var newName = CodeGenUtility.GetPropertyName(
                        bind.Transform.gameObject.name,
                        propertyNameMap
                    );
                    var bi = new BindInfo
                    {
                        typeName = bind.TypeName,
                        memberName = newName,
                        bindScript = bind,
                        pathToRoot = PathToParent(bind.Transform, task.gameObject.name),
                    };
                    task.bindInfos.Add(bi);
                }
        }

        private static string PathToParent(Transform trans, string parentName)
        {
            var retValue = new StringBuilder(trans.name);

            while (trans.parent != null)
            {
                if (trans.parent.name.Equals(parentName))
                    break;

                retValue = retValue.Insert(0, "/").Insert(0, trans.parent.name);

                trans = trans.parent;
            }

            return retValue.ToString();
        }
    }
}
