using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Lazy.UI.Basic
{
    public abstract class ABSBind : MonoBehaviour, IBind
    {
        /// <summary>
        /// * 标记类型
        /// </summary>
        [HideInInspector]
        public BindType markType = BindType.Default;

        /// <summary>
        /// * 自定义注释
        /// </summary>
        [HideInInspector]
        public string customComment;

        /// <summary>
        /// * 自定义组件名称 (新脚本)
        /// </summary>
        [HideInInspector]
        public string customComponentName;

        /// <summary>
        /// * 当前GameObject上要绑定的主脚本名称
        /// </summary>
        [HideInInspector]
        [SerializeField]
        private string componentName;

        public string Comment => customComment;
        public Transform Transform => transform;
        public BindType BindType => markType;

        public string TypeName
        {
            get
            {
                if (markType == BindType.Default)
                {
                    if (string.IsNullOrEmpty(componentName))
                        componentName = GetDefaultName();
                    else if (
                        !GetComponent(componentName)
                        && componentName != typeof(GameObject).FullName
                        && !GetComponent(componentName.Split(".").Last())
                    )
                        componentName = GetDefaultName();

                    return componentName;
                }

                if (markType == BindType.UICustom)
                    return customComponentName;
                return componentName;
            }
        }

        /// <summary>
        /// * 获取组件名称 (存在优先级)
        /// </summary>
        /// <returns></returns>
        private string GetDefaultName()
        {
            if (GetComponent<ViewPresenter>())
                return GetComponent<ViewPresenter>().GetType().FullName;

            // # spine
            if (GetComponent("SkeletonAnimation"))
                return "SkeletonAnimation";

            if (GetComponent<ScrollRect>())
                return "UnityEngine.UI.ScrollRect";
            if (GetComponent<InputField>())
                return "UnityEngine.UI.InputField";

            // # TMP
            if (GetComponent("TMP.TextMeshProUGUI"))
                return "TMP.TextMeshProUGUI";
            if (GetComponent("TMPro.TextMeshProUGUI"))
                return "TMPro.TextMeshProUGUI";
            if (GetComponent("TMPro.TextMeshPro"))
                return "TMPro.TextMeshPro";
            if (GetComponent("TMPro.TMP_InputField"))
                return "TMPro.TMP_InputField";

            // # UGUI
            if (GetComponent<Dropdown>())
                return "UnityEngine.UI.Dropdown";
            if (GetComponent<Button>())
                return "UnityEngine.UI.Button";
            if (GetComponent<Text>())
                return "UnityEngine.UI.Text";
            if (GetComponent<RawImage>())
                return "UnityEngine.UI.RawImage";
            if (GetComponent<Toggle>())
                return "UnityEngine.UI.Toggle";
            if (GetComponent<Slider>())
                return "UnityEngine.UI.Slider";
            if (GetComponent<Scrollbar>())
                return "UnityEngine.UI.Scrollbar";
            if (GetComponent<Image>())
                return "UnityEngine.UI.Image";
            if (GetComponent<ToggleGroup>())
                return "UnityEngine.UI.ToggleGroup";

            // # Physics
            if (GetComponent<Rigidbody>())
                return "Rigidbody";
            if (GetComponent<Rigidbody2D>())
                return "Rigidbody2D";
            if (GetComponent<BoxCollider2D>())
                return "BoxCollider2D";
            if (GetComponent<BoxCollider>())
                return "BoxCollider";
            if (GetComponent<CircleCollider2D>())
                return "CircleCollider2D";
            if (GetComponent<SphereCollider>())
                return "SphereCollider";
            if (GetComponent<MeshCollider>())
                return "MeshCollider";
            if (GetComponent<Collider>())
                return "Collider";
            if (GetComponent<Collider2D>())
                return "Collider2D";

            // # Other
            if (GetComponent<Animator>())
                return "Animator";
            if (GetComponent<Canvas>())
                return "Canvas";
            if (GetComponent<Camera>())
                return "Camera";
            if (GetComponent<RectTransform>())
                return "RectTransform";
            if (GetComponent<MeshRenderer>())
                return "MeshRenderer";

            if (GetComponent<SpriteRenderer>())
                return "SpriteRenderer";
            // # 默认使用Transform作为绑定
            return "Transform";
        }
    }
}
