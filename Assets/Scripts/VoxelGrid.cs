using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Voxel
{

    [SelectionBase]
    public class VoxelGrid : MonoBehaviour
    {
        [SerializeField]
        private float _gridSize = 1.0f;
        [SerializeField]
        private int _resolution = 5;
        private float _voxelSize;

        private bool[] _grid;
        private Material[] _voxelMaterials;

        [SerializeField]
        private GameObject _voxelPrefab;

        private static string[] _fillTypeNames = { "Filled", "Empty" };
        private int _fillTypeIndex;

        private void Awake()
        {
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            box.size = new Vector3(_gridSize, _gridSize, _gridSize);

            Initialize(_resolution, _gridSize);
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                RaycastHit hit;
                if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit))
                {
                    if (hit.collider.gameObject == gameObject)
                    {
                        EditVoxels(transform.InverseTransformPoint(hit.point));
                    }
                }
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(4f, 4f, 150f, 500f));
            GUILayout.Label("Fill Type");
            _fillTypeIndex = GUILayout.SelectionGrid(_fillTypeIndex, _fillTypeNames, 2);
            GUILayout.EndArea();
        }

        void Initialize(int resolution, float size)
        {
            _resolution = resolution;
            _voxelSize = size / _resolution;
            _grid = new bool[_resolution * _resolution * _resolution];
            _voxelMaterials = new Material[_grid.Length];

            // Fill grid
            for (int k = 0; k < _resolution; ++k)
            {
                for (int i = 0; i < _resolution; ++i)
                {
                    for (int j = 0; j < _resolution; ++j)
                    {
                        _grid[i + j * _resolution + k * _resolution * _resolution] = true;
                        if (_grid[i + j * _resolution])
                        {
                            InstantiateVoxel(j, i, k);
                        }
                    }
                }
            }

            SetGridColors();
        }

        void InstantiateVoxel(int x, int y, int z)
        {
            GameObject gameObj = Instantiate(_voxelPrefab, transform);
            Vector3 offset = _resolution % 2 == 0 ? new Vector3(0.5f, 0.5f, 0.5f) : Vector3.zero;
            gameObj.transform.localPosition = new Vector3((x + offset.x - _resolution / 2) * _voxelSize, (y + offset.y - _resolution / 2) * _voxelSize, (z + offset.z - _resolution / 2) * _voxelSize);
            gameObj.transform.localScale = Vector3.one * _voxelSize * 0.95f;
            _voxelMaterials[GetIndex(x, y, z)] = gameObj.GetComponent<MeshRenderer>().material;
        }

        void EditVoxels(Vector3 point)
        {
            int voxelX = (int)(point.x / _voxelSize + _resolution / 2);
            int voxelY = (int)(point.y / _voxelSize + _resolution / 2);
            int voxelZ = (int)(point.z / _voxelSize + _resolution / 2);
            //print("Grid Coord: " + voxelX + "," + voxelY);

            VoxelStencil stencil = new VoxelStencil();
            stencil._fillType = _fillTypeIndex == 0 ? false : true;
            Apply(voxelX, voxelY, voxelZ, stencil);
        }

        void Apply(int x, int y, int z, VoxelStencil stencil)
        {
            _grid[GetIndex(x, y, z)] = stencil.Apply(x, y, z);
            SetVoxelColor(x, y, z);
        }

        void SetVoxelColor(int x, int y, int z)
        {
            _voxelMaterials[GetIndex(x,y,z)].color = _grid[GetIndex(x, y, z)] ? Color.white : Color.black;
        }

        void SetGridColors()
        {
            for (int i = 0; i < _grid.Length; ++i)
            {
                _voxelMaterials[i].color = _grid[i] ? Color.white : Color.black;
            }
        }

        int GetIndex(int x, int y, int z)
        {
            return x + y * _resolution + z * _resolution * _resolution;
        }
    }
}
