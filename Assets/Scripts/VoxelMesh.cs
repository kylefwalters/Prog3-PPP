using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Voxel
{
    public class VoxelMesh : MonoBehaviour
    {
        [SerializeField]
        private Vector3Int _dimentions = new Vector3Int(10, 10, 10);
        private byte[,,] _voxels;

        private void Awake()
        {
            _voxels = new byte[_dimentions.x, _dimentions.y, _dimentions.z];
            FillGrid();
            GenerateMesh();
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
                        EditVoxel(transform.InverseTransformPoint(hit.point), 0);
                    }
                }
            }
            else if (Input.GetMouseButtonDown(1))
            {
                RaycastHit hit;
                if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit))
                {
                    if (hit.collider.gameObject == gameObject)
                    {
                        EditVoxel(transform.InverseTransformPoint(hit.point), 1);
                    }
                }
            }
        }

        void FillGrid()
        {
            for (int x = 1; x < _dimentions.x - 1; ++x)
            {
                for (int y = 1; y < _dimentions.y - 1; ++y)
                {
                    for (int z = 1; z < _dimentions.y - 1; ++z)
                    {
                        //_voxels[x, y, z] = z % 2 > 0 ? (byte)1 : (byte)0;
                        _voxels[x, y, z] = (byte)1;
                    }
                    //_voxels[x, y, 1] = (byte)1;
                }
            }
        }

        void EditVoxel(Vector3 point, byte newValue)
        {
            int voxelX = Mathf.RoundToInt(point.x);
            int voxelY = Mathf.RoundToInt(point.y);
            int voxelZ = Mathf.RoundToInt(point.z);

            print("Hit Point: " + point);
            print("Grid Coord: " + "(" + voxelX + "," + voxelY + "," + voxelZ + ")");

            byte selectedVoxel = _voxels[voxelX, voxelY, voxelZ];
            if (selectedVoxel == newValue)
            {
                Vector3Int alternativeVoxel = new Vector3Int(RoundToIntAltDown(point.x), RoundToIntAltDown(point.y), RoundToIntAltDown(point.z));
                if (alternativeVoxel == new Vector3Int(voxelX, voxelY, voxelZ))
                {
                    alternativeVoxel = new Vector3Int(RoundToIntAltUp(point.x), RoundToIntAltUp(point.y), RoundToIntAltUp(point.z));
                }
                print("Alt Grid Coord: " + alternativeVoxel);
                // Try adjacent voxel
                selectedVoxel = _voxels[alternativeVoxel.x, alternativeVoxel.y, alternativeVoxel.z];
                if (selectedVoxel != newValue && alternativeVoxel.x != 0 && alternativeVoxel.x != _dimentions.x - 1 &&
                    alternativeVoxel.y != 0 && alternativeVoxel.y != _dimentions.y - 1 &&
                    alternativeVoxel.z != 0 && alternativeVoxel.z != _dimentions.z - 1)
                {
                    _voxels[alternativeVoxel.x, alternativeVoxel.y, alternativeVoxel.z] = newValue;
                    GenerateMesh();
                }
            }
            else if(voxelX != 0 && voxelX != _dimentions.x - 1 &&
                    voxelY != 0 && voxelY != _dimentions.y - 1 &&
                    voxelZ != 0 && voxelZ != _dimentions.z - 1)
            {
                _voxels[voxelX, voxelY, voxelZ] = newValue;
                GenerateMesh();
            }
        }

        void GenerateMesh()
        {
            List<int> meshTriangles = new List<int>();
            List<Vector3> meshVerticies = new List<Vector3>();
            List<Vector2> meshuv = new List<Vector2>();
            Vector3[] vertPos = new Vector3[8]
            {
            new Vector3(-1, 1, -1), new Vector3(-1, 1, 1),
            new Vector3(1, 1, 1), new Vector3(1, 1, -1),
            new Vector3(-1, -1, -1), new Vector3(-1, -1, 1),
            new Vector3(1, -1, 1), new Vector3(1, -1, -1),
            };

            int[,] Faces = new int[6, 9]
            {
            {0, 1, 2, 3, 0, 1, 0, 0, 0},     //top
            {7, 6, 5, 4, 0, -1, 0, 1, 0},    //bottom
            {2, 1, 5, 6, 0, 0, 1, 1, 1},     //right
            {0, 3, 7, 4, 0, 0, -1,  1, 1},   //left
            {3, 2, 6, 7, 1, 0, 0,  1, 1},    //front
            {1, 0, 4, 5, -1, 0, 0,  1, 1}    //back
            };

            void AddQuad(int facenum, int v, int x, int y, int z)
            {
                // Add Mesh
                for (int i = 0; i < 4; i++)
                {
                    meshVerticies.Add(new Vector3(x, y, z) + vertPos[Faces[facenum, i]] / 2f);
                }
                meshTriangles.AddRange(new List<int>() { v, v + 1, v + 2, v, v + 2, v + 3 });

                // Add uvs
                Vector2 bottomleft = new Vector2(Faces[facenum, 7], Faces[facenum, 8]) / 2f;

                meshuv.AddRange(new List<Vector2>() { bottomleft + new Vector2(0, 0.5f), bottomleft + new Vector2(0.5f, 0.5f), bottomleft + new Vector2(0.5f, 0), bottomleft });
            }

            // Generate faces
            for (int x = 1; x < _dimentions.x - 1; x++)
            {
                for (int y = 1; y < _dimentions.y - 1; y++)
                {
                    for (int z = 1; z < _dimentions.z - 1; z++)
                    {
                        if (_voxels[x, y, z] == 1)
                        {
                            for (int o = 0; o < 6; o++)
                            {
                                if (_voxels[x + Faces[o, 4], y + Faces[o, 5], z + Faces[o, 6]] == 0)
                                {
                                    AddQuad(o, meshVerticies.Count, x, y, z);
                                }
                            }
                        }
                    }
                }
            }

            GetComponent<MeshFilter>().mesh = new Mesh()
            {
                vertices = meshVerticies.ToArray(),
                triangles = meshTriangles.ToArray(),
                uv = meshuv.ToArray()
            };

            // Generate Collision
            Mesh mesh = GetComponent<MeshFilter>().mesh;
            mesh.RecalculateBounds();
            gameObject.GetComponent<MeshCollider>().sharedMesh = mesh;
        }

        // Similar to Mathf.RoundToInt, but rounds down for 0.5f
        static int RoundToIntAltDown(float num)
        {
            if (Mathf.Abs((num % 1) - 0.5f) < 0.01f)
            {
                int numI = (int)num;
                return numI;
            }
            else
            {
                return Mathf.RoundToInt(num);
            }
        }
        // Similar to Mathf.RoundToInt, but rounds down for 0.5f
        static int RoundToIntAltUp(float num)
        {
            if (Mathf.Abs((num % 1) - 0.5f) < 0.01f)
            {
                int numI = (int)num + 1;
                return numI;
            }
            else
            {
                return Mathf.RoundToInt(num);
            }
        }
    }
}
