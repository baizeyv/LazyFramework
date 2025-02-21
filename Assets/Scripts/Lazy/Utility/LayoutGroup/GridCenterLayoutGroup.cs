using UnityEngine;
using UnityEngine.UI;

namespace Lazy.Utility
{
    [AddComponentMenu("Layout/Grid Center Layout Group", 999)]
    public class GridCenterLayoutGroup : LayoutGroup
    {
        public enum Corner
        {
            /// <summary>
            /// * 左上角
            /// </summary>
            UpperLeft = 0,

            /// <summary>
            /// * 右上角
            /// </summary>
            UpperRight = 1,

            /// <summary>
            /// * 左下角
            /// </summary>
            LowerLeft = 2,

            /// <summary>
            /// * 右下角
            /// </summary>
            LowerRight = 3,
        }

        public enum Axis
        {
            Horizontal = 0,
            Vertical = 1,
        }

        public enum Constraint
        {
            Flexible = 0,
            FixedColumnCount = 1,
            FixedRowCount = 2,
        }

        [SerializeField]
        protected Corner mStartCorner = Corner.UpperLeft;

        public Corner startCorner
        {
            get { return mStartCorner; }
            set { SetProperty(ref mStartCorner, value); }
        }

        [SerializeField]
        protected Axis mStartAxis = Axis.Horizontal;

        public Axis startAxis
        {
            get { return mStartAxis; }
            set { SetProperty(ref mStartAxis, value); }
        }

        [SerializeField]
        protected Vector2 mCellSize = new Vector2(100, 100);

        public Vector2 cellSize
        {
            get { return mCellSize; }
            set { SetProperty(ref mCellSize, value); }
        }

        [SerializeField]
        protected Vector2 mSpacing = Vector2.zero;

        public Vector2 spacing
        {
            get { return mSpacing; }
            set { SetProperty(ref mSpacing, value); }
        }

        [SerializeField]
        protected Constraint mConstraint = Constraint.Flexible;

        public Constraint constraint
        {
            get { return mConstraint; }
            set { SetProperty(ref mConstraint, value); }
        }

        [SerializeField]
        protected int mConstraintCount = 2;

        public int constraintCount
        {
            get { return mConstraintCount; }
            set { SetProperty(ref mConstraintCount, Mathf.Max(value, 1)); }
        }

        protected GridCenterLayoutGroup() { }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            constraintCount = constraintCount;
        }
#endif

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();

            int minColumns = 0;
            int preferredColumns = 0;
            if (mConstraint == Constraint.FixedColumnCount)
            {
                minColumns = preferredColumns = mConstraintCount;
            }
            else if (mConstraint == Constraint.FixedRowCount)
            {
                minColumns = preferredColumns = Mathf.CeilToInt(
                    rectChildren.Count / (float)mConstraintCount - 0.001f
                );
            }
            else
            {
                minColumns = 1;
                preferredColumns = Mathf.CeilToInt(Mathf.Sqrt(rectChildren.Count));
            }

            SetLayoutInputForAxis(
                padding.horizontal + (cellSize.x + spacing.x) * minColumns - spacing.x,
                padding.horizontal + (cellSize.x + spacing.x) * preferredColumns - spacing.x,
                -1,
                0
            );
        }

        public override void CalculateLayoutInputVertical()
        {
            int minRows = 0;
            if (mConstraint == Constraint.FixedColumnCount)
            {
                minRows = Mathf.CeilToInt(rectChildren.Count / (float)mConstraintCount - 0.001f);
            }
            else if (mConstraint == Constraint.FixedRowCount)
            {
                minRows = mConstraintCount;
            }
            else
            {
                float width = rectTransform.rect.width;
                int cellCountX = Mathf.Max(
                    1,
                    Mathf.FloorToInt(
                        (width - padding.horizontal + spacing.x + 0.001f) / (cellSize.x + spacing.x)
                    )
                );
                minRows = Mathf.CeilToInt(rectChildren.Count / (float)cellCountX);
            }

            float minSpace = padding.vertical + (cellSize.y + spacing.y) * minRows - spacing.y;
            SetLayoutInputForAxis(minSpace, minSpace, -1, 1);
        }

        public override void SetLayoutHorizontal()
        {
            SetCellsAlongAxis(0);
        }

        public override void SetLayoutVertical()
        {
            SetCellsAlongAxis(1);
        }

        private void SetCellsAlongAxis(int axis)
        {
            // Normally a Layout Controller should only set horizontal values when invoked for the horizontal axis
            // and only vertical values when invoked for the vertical axis.
            // However, in this case we set both the horizontal and vertical position when invoked for the vertical axis.
            // Since we only set the horizontal position and not the size, it shouldn't affect children's layout,
            // and thus shouldn't break the rule that all horizontal layout must be calculated before all vertical layout.
            var rectChildrenCount = rectChildren.Count;
            if (axis == 0)
            {
                // Only set the sizes when invoked for horizontal axis, not the positions.

                for (int i = 0; i < rectChildrenCount; i++)
                {
                    RectTransform rect = rectChildren[i];

                    m_Tracker.Add(
                        this,
                        rect,
                        DrivenTransformProperties.Anchors
                            | DrivenTransformProperties.AnchoredPosition
                            | DrivenTransformProperties.SizeDelta
                    );

                    rect.anchorMin = Vector2.up;
                    rect.anchorMax = Vector2.up;
                    rect.sizeDelta = cellSize;
                }
                return;
            }

            float width = rectTransform.rect.size.x;
            float height = rectTransform.rect.size.y;

            int cellCountX = 1;
            int cellCountY = 1;
            if (mConstraint == Constraint.FixedColumnCount)
            {
                cellCountX = mConstraintCount;

                if (rectChildrenCount > cellCountX)
                    cellCountY =
                        rectChildrenCount / cellCountX
                        + (rectChildrenCount % cellCountX > 0 ? 1 : 0);
            }
            else if (mConstraint == Constraint.FixedRowCount)
            {
                cellCountY = mConstraintCount;

                if (rectChildrenCount > cellCountY)
                    cellCountX =
                        rectChildrenCount / cellCountY
                        + (rectChildrenCount % cellCountY > 0 ? 1 : 0);
            }
            else
            {
                if (cellSize.x + spacing.x <= 0)
                    cellCountX = int.MaxValue;
                else
                    cellCountX = Mathf.Max(
                        1,
                        Mathf.FloorToInt(
                            (width - padding.horizontal + spacing.x + 0.001f)
                                / (cellSize.x + spacing.x)
                        )
                    );

                if (cellSize.y + spacing.y <= 0)
                    cellCountY = int.MaxValue;
                else
                    cellCountY = Mathf.Max(
                        1,
                        Mathf.FloorToInt(
                            (height - padding.vertical + spacing.y + 0.001f)
                                / (cellSize.y + spacing.y)
                        )
                    );
            }

            int cornerX = (int)startCorner % 2;
            int cornerY = (int)startCorner / 2;

            int cellsPerMainAxis,
                actualCellCountX,
                actualCellCountY;
            if (startAxis == Axis.Horizontal)
            {
                cellsPerMainAxis = cellCountX;
                actualCellCountX = Mathf.Clamp(cellCountX, 1, rectChildrenCount);
                actualCellCountY = Mathf.Clamp(
                    cellCountY,
                    1,
                    Mathf.CeilToInt(rectChildrenCount / (float)cellsPerMainAxis)
                );
            }
            else
            {
                cellsPerMainAxis = cellCountY;
                actualCellCountY = Mathf.Clamp(cellCountY, 1, rectChildrenCount);
                actualCellCountX = Mathf.Clamp(
                    cellCountX,
                    1,
                    Mathf.CeilToInt(rectChildrenCount / (float)cellsPerMainAxis)
                );
            }

            Vector2 requiredSpace = new Vector2(
                actualCellCountX * cellSize.x + (actualCellCountX - 1) * spacing.x,
                actualCellCountY * cellSize.y + (actualCellCountY - 1) * spacing.y
            );
            Vector2 startOffset = new Vector2(
                GetStartOffset(0, requiredSpace.x),
                GetStartOffset(1, requiredSpace.y)
            );

            for (int i = 0; i < rectChildrenCount; i++)
            {
                int positionX;
                int positionY;
                if (startAxis == Axis.Horizontal)
                {
                    positionX = i % cellsPerMainAxis;
                    positionY = i / cellsPerMainAxis;
                }
                else
                {
                    positionX = i / cellsPerMainAxis;
                    positionY = i % cellsPerMainAxis;
                }

                if (cornerX == 1)
                    positionX = actualCellCountX - 1 - positionX;
                if (cornerY == 1)
                    positionY = actualCellCountY - 1 - positionY;

                GetAxisOffset(
                    i,
                    cellCountX,
                    cellCountY,
                    rectChildrenCount,
                    out var offsetX,
                    out var offsetY
                );

                // TODO: Position set center
                SetChildAlongAxis( // ? 水平轴
                    rectChildren[i],
                    0,
                    startOffset.x + (cellSize[0] + spacing[0]) * positionX + offsetX,
                    cellSize[0]
                );
                SetChildAlongAxis( // ? 垂直轴
                    rectChildren[i],
                    1,
                    startOffset.y + (cellSize[1] + spacing[1]) * positionY + offsetY,
                    cellSize[1]
                );
            }
        }

        /// <summary>
        /// * 根据元素索引获取所在行和列的偏移量
        /// </summary>
        /// <param name="index"></param>
        private void GetAxisOffset(
            int index,
            int cellCountX,
            int cellCountY,
            int totalCount,
            out float offsetX,
            out float offsetY
        )
        {
            if (totalCount == 1)
            {
                offsetX = 0;
                offsetY = 0;
                return;
            }
            if (startAxis == 0)
            {
                var rowIndex = index / cellCountX;
                var maxRowIndex = (totalCount - 1) / cellCountX;
                if (rowIndex < maxRowIndex)
                {
                    offsetX = 0;
                    offsetY = 0;
                    return;
                }
                var totalInRowIndex = (totalCount - 1) % cellCountX;
                if (totalInRowIndex + 1 == cellCountX)
                {
                    offsetX = 0;
                    offsetY = 0;
                    return;
                }
                var inRowIndex = index % cellCountX;
                // * 最后一行缺少的元素数量
                var offsetCount = cellCountX - (totalInRowIndex + 1);
                offsetX = cellSize.x / 2f * offsetCount + spacing.x / 2f * offsetCount;
                offsetY = 0;
            }
            else
            {
                var colIndex = index / cellCountY;
                var maxColIndex = (totalCount - 1) / cellCountY;
                if (colIndex < maxColIndex)
                {
                    offsetX = 0;
                    offsetY = 0;
                    return;
                }
                var totalInColIndex = (totalCount - 1) % cellCountY;
                if (totalInColIndex + 1 == cellCountY)
                {
                    offsetX = 0;
                    offsetY = 0;
                    return;
                }
                // * 最后一列缺少的元素数量
                var offsetCount = cellCountY - (totalInColIndex + 1);
                offsetY = cellSize.y / 2f * offsetCount + spacing.y / 2f * offsetCount;
                offsetX = 0;
            }
        }
    }
}