using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Voxel
{
    public class VoxelMesh : MonoBehaviour
    {
        [SerializeField]
        private Vector3Int _dimentions = new Vector3Int(10, 10, 10);

        enum VoxelTypes
        {
            Empty,
            Whole,
            SlopeForward,
            SlopeRight,
            SlopeBack,
            SlopeLeft,
            SlopeForwardInv,
            SlopeBackInv,
            SlopeRightInv,
            SlopeLeftInv,
            CornerForwardRight,
            CornerBackRight,
            CornerBackLeft,
            CornerForwardLeft,
            CornerForwardRightInv,
            CornerBackRightInv,
            CornerBackLeftInv,
            CornerForwardLeftInv,
            SlopeHorizontalForward,
            SlopeHorizontalRight,
            SlopeHorizontalBack,
            SlopeHorizontalLeft
        }
        private VoxelTypes[,,] _voxels;

        private void Awake()
        {
            _voxels = new VoxelTypes[_dimentions.x, _dimentions.y, _dimentions.z];
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
                        EditVoxel(transform.InverseTransformPoint(hit.point), (VoxelTypes)1);
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
                        _voxels[x, y, z] = (VoxelTypes)1;
                    }
                    //_voxels[x, y, 1] = (byte)1;
                }
            }
        }

        void EditVoxel(Vector3 point, VoxelTypes newValue)
        {
            int voxelX = Mathf.RoundToInt(point.x);
            int voxelY = Mathf.RoundToInt(point.y);
            int voxelZ = Mathf.RoundToInt(point.z);

            //print("Hit Point: " + point);
            //print("Grid Coord: " + "(" + voxelX + "," + voxelY + "," + voxelZ + ")");

            VoxelTypes selectedVoxel = _voxels[voxelX, voxelY, voxelZ];
            if (selectedVoxel == newValue)
            {
                Vector3Int alternativeVoxel = new Vector3Int(RoundToIntAltDown(point.x), RoundToIntAltDown(point.y), RoundToIntAltDown(point.z));
                if (alternativeVoxel == new Vector3Int(voxelX, voxelY, voxelZ))
                {
                    alternativeVoxel = new Vector3Int(RoundToIntAltUp(point.x), RoundToIntAltUp(point.y), RoundToIntAltUp(point.z));
                }
                //print("Alt Grid Coord: " + alternativeVoxel);
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
            else 
            {
                _voxels[voxelX, voxelY, voxelZ] = newValue;
                GenerateMesh();
            }
        }

        void GenerateMesh()
        {
            MeshMarch();

            List<int> meshTriangles = new List<int>();
            List<Vector3> meshVerticies = new List<Vector3>();
            List<Vector2> meshuv = new List<Vector2>();
            // Used to offset vertices from voxel coordinates
            Vector3[] vertPos = new Vector3[8]
            {
            new Vector3(-1, 1, -1) /*TBL*/, new Vector3(-1, 1, 1), /*TFL*/
            new Vector3(1, 1, 1) /*TFR*/, new Vector3(1, 1, -1), /*TBR*/
            new Vector3(-1, -1, -1) /*BBL*/, new Vector3(-1, -1, 1), /*BFL*/
            new Vector3(1, -1, 1) /*BFR*/, new Vector3(1, -1, -1), /*BBR*/
            };

            int[,] Faces = new int[32, 9]
            {
            // 0~3: Used for getting VertPos
            // 4~6: Used for checking neighbouring voxels
            // 7~8: Used for uvs
            {0,1,2,3,0,1,0,0,0},            // Top
            {7,6,5,4,0,-1,0,1,0},           // Bottom
            {2,1,5,6,0,0,1,1,1},            // Front
            {0,3,7,4,0,0,-1,1,1},           // Back
            {3,2,6,7,1,0,0,1,1},            // Right
            {1,0,4,5,-1,0,0,1,1},           // Left
            {0,5,6,3,0,0,0,1,1},            // Slope Forward
            {1,6,7,0,0,0,0,1,1},            // Slope Right
            {1,2,7,4,0,0,0,1,1},            // Slope Back
            {2,3,4,5,0,0,0,1,1},            // Slope Left
            {2,1,4,7,0,0,0,1,1},            // Slope Forward Inverted
            {2,5,4,3,0,0,0,1,1},            // Slope Right Inverted
            {3,6,5,0,0,0,0,1,1},            // Slope Back Inverted
            {0,7,6,1,0,0,0,1,1},            // Slope Left Inverted
            {3,4,6,0,0,0,0,1,1},            // Corner Forward Right
            {1,6,4,0,0,0,0,1,1},            // Corner Back Right
            {5,2,7,4,0,0,0,1,1},            // Corner Back Left
            {0,5,7,0,0,0,0,1,1},            // Corner Forward Left
            {6,1,3,0,0,0,0,1,1},            // Corner Forward Right Inverted
            {5,0,2,0,0,0,0,1,1},            // Corner Back Right Inverted
            {4,3,1,0,0,0,0,1,1},            // Corner Back Left Inverted
            {7,2,0,0,0,0,0,1,1},            // Corner Forward Left Inverted
            {5,1,3,7,0,0,0,1,1},            // Slope Horizontal Forward
            {4,0,2,6,0,0,0,1,1},            // Slope Horizontal Right
            {3,1,5,7,0,0,0,1,1},            // Slope Horizontal Down
            {2,0,4,6,0,0,0,1,1},            // Slope Horizontal Left
            {3,0,1,2,0,1,0,0,0},            // Top Alternate
            {4,7,6,5,0,-1,0,1,0},           // Bottom Alternate
            {6,2,1,5,0,0,1,1,1},            // Front Alternate
            {4,0,3,7,0,0,-1,1,1},           // Back Alternate
            {7,3,2,6,1,0,0,1,1},            // Right Alternate
            {5,1,0,4,-1,0,0,1,1}            // Left Alternate
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
            void AddTri(int facenum, int v, int x, int y, int z, bool getOther = false, bool isInverted = false)
            {
                // Add Mesh
                for (int i = 0; i < 3; i++)
                {
                    meshVerticies.Add(new Vector3(x, y, z) + vertPos[Faces[facenum, i]] / 2f);
                }
                List<int> verts;
                if (isInverted)
                {
                    verts = getOther ? new List<int>() { v, v + 3, v + 2 } : new List<int>() { v, v + 2, v + 1 };
                }
                else
                {
                    verts = getOther ? new List<int>() { v, v + 2, v + 3 } : new List<int>() { v, v + 1, v + 2 };
                }
                meshTriangles.AddRange(verts);

                // Add uvs
                Vector2 bottomleft = new Vector2(Faces[facenum, 7], Faces[facenum, 8]) / 2f;

                meshuv.AddRange(new List<Vector2>() { bottomleft + new Vector2(0, 0.5f), bottomleft + new Vector2(0.5f, 0.5f), bottomleft + new Vector2(0.5f, 0) });
            }

            // Generate faces
            VoxelTypes currentType;
            for (int x = 1; x < _dimentions.x - 1; x++)
            {
                for (int y = 1; y < _dimentions.y - 1; y++)
                {
                    for (int z = 1; z < _dimentions.z - 1; z++)
                    {
                        switch (_voxels[x, y, z])
                        {
                            case VoxelTypes.Whole:
                            default:
                                for (int o = 0; o < 6; o++)
                                {
                                    if (_voxels[x + Faces[o, 4], y + Faces[o, 5], z + Faces[o, 6]] == 0)
                                    {
                                        AddQuad(o, meshVerticies.Count, x, y, z);
                                    }
                                }
                                break;
                            case VoxelTypes.SlopeForward:
                                AddQuad(6, meshVerticies.Count, x, y, z);
                                if (_voxels[x + Faces[4, 4], y + Faces[4, 5], z + Faces[4, 6]] == VoxelTypes.Whole)
                                {
                                    AddTri(4, meshVerticies.Count, x, y, z, false, true);
                                }
                                if (_voxels[x + Faces[5, 4], y + Faces[5, 5], z + Faces[5, 6]] == VoxelTypes.Whole)
                                {
                                    AddTri(31, meshVerticies.Count, x, y, z, false, true);
                                }
                                break;
                            case VoxelTypes.SlopeRight:
                                AddQuad(7, meshVerticies.Count, x, y, z);
                                if (_voxels[x + Faces[2, 4], y + Faces[2, 5], z + Faces[2, 6]] == VoxelTypes.Whole)
                                {
                                    AddTri(28, meshVerticies.Count, x, y, z, false, true);
                                }
                                if (_voxels[x + Faces[3, 4], y + Faces[3, 5], z + Faces[3, 6]] == VoxelTypes.Whole)
                                {
                                    AddTri(3, meshVerticies.Count, x, y, z, false, true);
                                }
                                break;
                            case VoxelTypes.SlopeBack:
                                AddQuad(8, meshVerticies.Count, x, y, z);
                                if (_voxels[x + Faces[4, 4], y + Faces[4, 5], z + Faces[4, 6]] == VoxelTypes.Whole)
                                {
                                    AddTri(30, meshVerticies.Count, x, y, z, false, true);
                                }
                                if (_voxels[x + Faces[5, 4], y + Faces[5, 5], z + Faces[5, 6]] == VoxelTypes.Whole)
                                {
                                    AddTri(5, meshVerticies.Count, x, y, z, false, true);
                                }
                                break;
                            case VoxelTypes.SlopeLeft:
                                AddQuad(9, meshVerticies.Count, x, y, z);
                                if (_voxels[x + Faces[2, 4], y + Faces[2, 5], z + Faces[2, 6]] == VoxelTypes.Whole)
                                {
                                    AddTri(2, meshVerticies.Count, x, y, z, false, true);
                                }
                                if (_voxels[x + Faces[3, 4], y + Faces[3, 5], z + Faces[3, 6]] == VoxelTypes.Whole)
                                {
                                    AddTri(29, meshVerticies.Count, x, y, z, false, true);
                                }
                                break;
                            case VoxelTypes.SlopeForwardInv:
                                AddQuad(10, meshVerticies.Count, x, y, z);
                                break;
                            case VoxelTypes.SlopeRightInv:
                                AddQuad(11, meshVerticies.Count, x, y, z);
                                break;
                            case VoxelTypes.SlopeBackInv:
                                AddQuad(12, meshVerticies.Count, x, y, z);
                                break;
                            case VoxelTypes.SlopeLeftInv:
                                AddQuad(13, meshVerticies.Count, x, y, z);
                                break;
                            case VoxelTypes.CornerForwardRight:
                                AddTri(14, meshVerticies.Count, x, y, z);
                                break;
                            case VoxelTypes.CornerBackRight:
                                AddTri(15, meshVerticies.Count, x, y, z);
                                break;
                            case VoxelTypes.CornerBackLeft:
                                AddTri(16, meshVerticies.Count, x, y, z);
                                break;
                            case VoxelTypes.CornerForwardLeft:
                                AddTri(17, meshVerticies.Count, x, y, z);
                                break;
                            case VoxelTypes.CornerForwardRightInv:
                                AddTri(18, meshVerticies.Count, x, y, z);
                                break;
                            case VoxelTypes.CornerBackRightInv:
                                AddTri(19, meshVerticies.Count, x, y, z);
                                break;
                            case VoxelTypes.CornerBackLeftInv:
                                AddTri(20, meshVerticies.Count, x, y, z);
                                break;
                            case VoxelTypes.CornerForwardLeftInv:
                                AddTri(21, meshVerticies.Count, x, y, z);
                                break;
                            case VoxelTypes.SlopeHorizontalForward:
                                AddQuad(22, meshVerticies.Count, x, y, z);
                                break;
                            case VoxelTypes.SlopeHorizontalRight:
                                AddQuad(23, meshVerticies.Count, x, y, z);
                                break;
                            case VoxelTypes.SlopeHorizontalBack:
                                AddQuad(24, meshVerticies.Count, x, y, z);
                                break;
                            case VoxelTypes.SlopeHorizontalLeft:
                                AddQuad(25, meshVerticies.Count, x, y, z);
                                break;
                            case VoxelTypes.Empty:
                                break;
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

        void MeshMarch()
        {
            for (int x = 1; x < _dimentions.x - 1; ++x)
            {
                for (int y = 1; y < _dimentions.y - 1; ++y)
                {
                    for (int z = 1; z < _dimentions.y - 1; ++z)
                    {
                        if (_voxels[x, y, z] != VoxelTypes.Empty)
                        {
                            int voxelValue = 0;
                            if (_voxels[x + 1, y, z] != VoxelTypes.Empty)
                            {
                                voxelValue = voxelValue | 1;
                            }
                            if (_voxels[x - 1, y, z] != VoxelTypes.Empty)
                            {
                                voxelValue = voxelValue | 2;
                            }
                            if (_voxels[x, y + 1, z] != VoxelTypes.Empty)
                            {
                                voxelValue = voxelValue | 4;
                            }
                            if (_voxels[x, y - 1, z] != VoxelTypes.Empty)
                            {
                                voxelValue = voxelValue | 8;
                            }
                            if (_voxels[x, y, z + 1] != VoxelTypes.Empty)
                            {
                                voxelValue = voxelValue | 16;
                            }
                            if (_voxels[x, y, z - 1] != VoxelTypes.Empty)
                            {
                                voxelValue = voxelValue | 32;
                            }

                            switch (voxelValue)
                            {
                                default:
                                    _voxels[x, y, z] = VoxelTypes.Whole;
                                    break;
                                case 5:
                                case 53:
                                    _voxels[x, y, z] = VoxelTypes.SlopeLeftInv;
                                    break;
                                case 6:
                                case 54:
                                    _voxels[x, y, z] = VoxelTypes.SlopeRightInv;
                                    break;
                                case 9:
                                case 57:
                                    _voxels[x, y, z] = VoxelTypes.SlopeLeft;
                                    break;
                                case 10:
                                case 58:
                                    _voxels[x, y, z] = VoxelTypes.SlopeRight;
                                    break;
                                case 20:
                                case 36:
                                case 39:
                                    _voxels[x, y, z] = VoxelTypes.SlopeForwardInv;
                                    break;
                                case 21:
                                    _voxels[x, y, z] = VoxelTypes.CornerForwardRightInv;
                                    break;
                                case 22:
                                    _voxels[x, y, z] = VoxelTypes.CornerBackRightInv;
                                    break;
                                case 23:
                                case 24:
                                    _voxels[x, y, z] = VoxelTypes.SlopeBackInv;
                                    break;
                                case 25:
                                    _voxels[x, y, z] = VoxelTypes.CornerBackLeft;
                                    break;
                                case 38:
                                    _voxels[x, y, z] = VoxelTypes.CornerBackLeftInv;
                                    break;
                                case 26:
                                    _voxels[x, y, z] = VoxelTypes.CornerBackRight;
                                    break;
                                case 27:
                                    _voxels[x, y, z] = VoxelTypes.SlopeBack;
                                    break;
                                case 29:
                                    _voxels[x, y, z] = VoxelTypes.SlopeHorizontalForward;
                                    break;
                                case 30:
                                    _voxels[x, y, z] = VoxelTypes.SlopeHorizontalRight;
                                    break;
                                case 37:
                                    _voxels[x, y, z] = VoxelTypes.CornerForwardLeftInv;
                                    break;
                                case 40:
                                case 43:
                                    _voxels[x, y, z] = VoxelTypes.SlopeForward;
                                    break;
                                case 41:
                                    _voxels[x, y, z] = VoxelTypes.CornerForwardRight;
                                    break;
                                case 42:
                                    _voxels[x, y, z] = VoxelTypes.CornerForwardLeft;
                                    break;
                                case 45:
                                    _voxels[x, y, z] = VoxelTypes.SlopeHorizontalLeft;
                                    break;
                                case 46:
                                    _voxels[x, y, z] = VoxelTypes.SlopeHorizontalBack;
                                    break;
                            }

                        }
                    }
                }
            }
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
