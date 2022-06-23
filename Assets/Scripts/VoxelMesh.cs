using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Voxel
{
    public class VoxelMesh : MonoBehaviour
    {
        [SerializeField]
        private Vector3Int _dimentions = new Vector3Int(10, 10, 10);
        private Mesh _meshCollision;

        public enum VoxelTypes
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
        public class Voxel
        {
            public VoxelTypes _type = VoxelTypes.Empty;
            // Bitmask for which verts the voxel is occupying
            // 0b000000 = Empty
            // 0b000001 = Full
            // 0b000010 = Half Type 1 (0,1,2)
            // 0b000011 = Half Type 2 (0,2,3)
            // 0b000100 = Half Type 3 (1,2,3)
            // 0b000101 = Half Type 4 (1,3,0)
            public uint _occupiedVerts = 0;
        }
        private Voxel[,,] _voxels;

        private void Awake()
        {
            _voxels = new Voxel[_dimentions.x, _dimentions.y, _dimentions.z];
            for (int x = 0; x < _dimentions.x; ++x)
            {
                for (int y = 0; y < _dimentions.y; ++y)
                {
                    for (int z = 0; z < _dimentions.z; ++z)
                    {
                        _voxels[x, y, z] = new Voxel();
                    }
                }
            }

            FillGrid();
            MeshMarch();
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
                    for (int z = 1; z < _dimentions.z - 1; ++z)
                    {
                        _voxels[x, y, z]._type = (VoxelTypes)1;
                    }
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

            VoxelTypes selectedVoxel = _voxels[voxelX, voxelY, voxelZ]._type;
            if (selectedVoxel == newValue)
            {
                Vector3Int alternativeVoxel = new Vector3Int(RoundToIntAltDown(point.x), RoundToIntAltDown(point.y), RoundToIntAltDown(point.z));
                if (alternativeVoxel == new Vector3Int(voxelX, voxelY, voxelZ))
                {
                    alternativeVoxel = new Vector3Int(RoundToIntAltUp(point.x), RoundToIntAltUp(point.y), RoundToIntAltUp(point.z));
                }
                //print("Alt Grid Coord: " + alternativeVoxel);

                // Try adjacent voxel
                selectedVoxel = _voxels[alternativeVoxel.x, alternativeVoxel.y, alternativeVoxel.z]._type;
                if (selectedVoxel != newValue && alternativeVoxel.x != 0 && alternativeVoxel.x != _dimentions.x - 1 &&
                    alternativeVoxel.y != 0 && alternativeVoxel.y != _dimentions.y - 1 &&
                    alternativeVoxel.z != 0 && alternativeVoxel.z != _dimentions.z - 1)
                {
                    print(_voxels[alternativeVoxel.x, alternativeVoxel.y, alternativeVoxel.z]);
                    _voxels[alternativeVoxel.x, alternativeVoxel.y, alternativeVoxel.z]._type = newValue;
                    //for (int x = Mathf.Max(alternativeVoxel.x - 1, 1); x <= Mathf.Min(alternativeVoxel.x + 1, _dimentions.x - 1); ++x)
                    //{
                    //    for (int y = Mathf.Max(alternativeVoxel.y - 1, 1); y <= Mathf.Min(alternativeVoxel.y + 1, _dimentions.x - 1); ++y)
                    //    {
                    //        for (int z = Mathf.Max(alternativeVoxel.z - 1, 1); z <= Mathf.Min(alternativeVoxel.z + 1, _dimentions.x - 1); ++z)
                    //        {
                    //            BlockMarch(x, y, z);
                    //        }
                    //    }
                    //}
                    MeshMarch();
                    GenerateMesh();
                }
            }
            else if (voxelX != 0 && voxelX != _dimentions.x - 1 &&
                    voxelY != 0 && voxelY != _dimentions.y - 1 &&
                    voxelZ != 0 && voxelZ != _dimentions.z - 1)
            {
                print(_voxels[voxelX, voxelY, voxelZ]);
                _voxels[voxelX, voxelY, voxelZ]._type = newValue;
                //for (int x = Mathf.Max(voxelX - 1, 1); x <= Mathf.Min(voxelX + 1, _dimentions.x - 1); ++x)
                //{
                //    for (int y = Mathf.Max(voxelY - 1, 1); y <= Mathf.Min(voxelY + 1, _dimentions.x - 1); ++y)
                //    {
                //        for (int z = Mathf.Max(voxelZ - 1, 1); z <= Mathf.Min(voxelZ + 1, _dimentions.x - 1); ++z)
                //        {
                //            BlockMarch(x, y, z);
                //        }
                //    }
                //}
                MeshMarch();
                GenerateMesh();
            }
        }

        void GenerateMesh()
        {
            List<int> meshTriangles = new List<int>();
            List<Vector3> meshVerticies = new List<Vector3>();
            List<Vector2> meshuv = new List<Vector2>();
            List<int> meshTrianglesCollision = new List<int>();
            List<Vector3> meshVerticiesCollision = new List<Vector3>();
            List<Vector2> meshuvCollision = new List<Vector2>();
            int lastZ = 1;
            int currentRow = 1;
            int currentDepth = 1;
            int lastFace = int.MaxValue;
            // Store offset for first vertex of each voxel (Quad Only)
            int[,,] triOffsets = new int[_dimentions.x, _dimentions.y, _dimentions.z];
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
            // 0~3: Used for getting vertPos
            // 4~6: Used for checking neighbouring voxels
            // 7~8: Used for uv
            {0,1,2,3,0,1,0,0,0},            // Top
            {7,6,5,4,0,-1,0,1,0},           // Bottom
            {2,1,5,6,0,0,1,1,1},            // Front
            {0,3,7,4,0,0,-1,1,1},           // Back
            {3,2,6,7,1,0,0,1,1},            // Right
            {4,5,1,0,-1,0,0,1,1},           // Left
            {0,5,6,3,0,0,0,1,1},            // Slope Forward
            {0,1,6,7,0,0,0,1,1},            // Slope Right
            {1,2,7,4,0,0,0,1,1},            // Slope Back
            {4,5,2,3,0,0,0,1,1},            // Slope Left
            {2,1,4,7,0,0,0,1,1},            // Slope Forward Inverted
            {3,2,5,4,0,0,0,1,1},            // Slope Right Inverted
            {3,6,5,0,0,0,0,1,1},            // Slope Back Inverted
            {7,6,1,0,0,0,0,1,1},            // Slope Left Inverted
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

                // Add Collision Mesh
                if (lastFace == int.MaxValue)
                {
                    lastFace = facenum;
                    lastZ = z;
                    currentRow = y;
                    currentDepth = x;
                    meshVerticiesCollision.Add(new Vector3(x, y, z) + vertPos[Faces[facenum, 3]] / 2f);
                    meshVerticiesCollision.Add(new Vector3(x, y, z) + vertPos[Faces[facenum, 0]] / 2f);
                    meshTrianglesCollision.AddRange(new List<int>() { v, v + 1, v + 2, v, v + 2, v + 3 });
                }
                else if (lastFace != facenum || currentRow != y || currentDepth != x || z == _dimentions.z - 2)
                {
                    meshVerticiesCollision.Add(new Vector3(currentDepth, currentRow, lastZ) + vertPos[Faces[lastFace, 1]] / 2f);
                    meshVerticiesCollision.Add(new Vector3(currentDepth, currentRow, lastZ) + vertPos[Faces[lastFace, 2]] / 2f);
                    int vC = meshVerticiesCollision.Count;

                    lastFace = facenum;
                    currentRow = y;
                    currentDepth = x;

                    if (_voxels[x, y, z + 1]._type != VoxelTypes.Empty)
                    {
                        triOffsets[x, y, z] = vC;
                        meshTrianglesCollision.AddRange(new List<int>() { vC, vC + 1, vC + 2, vC, vC + 2, vC + 3 });
                        meshVerticiesCollision.Add(new Vector3(x, y, z) + vertPos[Faces[facenum, 3]] / 2f);
                        meshVerticiesCollision.Add(new Vector3(x, y, z) + vertPos[Faces[facenum, 0]] / 2f);
                    }
                    else
                    {
                        triOffsets[x, y, z] = vC;
                        meshTrianglesCollision.AddRange(new List<int>() { vC, vC + 1, vC + 2, vC, vC + 2, vC + 3 });
                        for (int i = 1; i < 5; ++i)
                        {
                            meshVerticiesCollision.Add(new Vector3(x, y, z) + vertPos[Faces[facenum, i % 4]] / 2f);
                        }
                    }
                }
                lastZ = z;
            }
            void AddTri(int facenum, int v, int x, int y, int z, bool getOther = false, bool isInverted = false)
            {
                if (lastFace != int.MaxValue)
                {
                    meshVerticiesCollision.Add(new Vector3(currentDepth, currentRow, lastZ) + vertPos[Faces[lastFace, 1]] / 2f);
                    meshVerticiesCollision.Add(new Vector3(currentDepth, currentRow, lastZ) + vertPos[Faces[lastFace, 2]] / 2f);
                }
                int vC = meshVerticiesCollision.Count;
                // Add Mesh
                if (getOther)
                {
                    meshVerticies.Add(new Vector3(x, y, z) + vertPos[Faces[facenum, 0]] / 2f);
                    meshVerticies.Add(new Vector3(x, y, z) + vertPos[Faces[facenum, 2]] / 2f);
                    meshVerticies.Add(new Vector3(x, y, z) + vertPos[Faces[facenum, 3]] / 2f);
                    meshVerticiesCollision.Add(new Vector3(x, y, z) + vertPos[Faces[facenum, 0]] / 2f);
                    meshVerticiesCollision.Add(new Vector3(x, y, z) + vertPos[Faces[facenum, 2]] / 2f);
                    meshVerticiesCollision.Add(new Vector3(x, y, z) + vertPos[Faces[facenum, 3]] / 2f);
                }
                else
                {
                    for (int i = 0; i < 3; i++)
                    {
                        meshVerticies.Add(new Vector3(x, y, z) + vertPos[Faces[facenum, i]] / 2f);
                        meshVerticiesCollision.Add(new Vector3(x, y, z) + vertPos[Faces[facenum, i]] / 2f);
                    }
                }
                List<int> verts;
                List<int> vertsCollision;
                if (isInverted)
                {
                    verts = new List<int>() { v, v + 2, v + 1 };
                    vertsCollision = new List<int>() { vC, vC + 1, vC + 2 };
                }
                else
                {
                    verts = new List<int>() { v, v + 1, v + 2 };
                    vertsCollision = new List<int>() { vC, vC + 1, vC + 2 };
                }
                meshTriangles.AddRange(verts);
                meshTrianglesCollision.AddRange(vertsCollision);

                // Add uvs
                Vector2 bottomleft = new Vector2(Faces[facenum, 7], Faces[facenum, 8]) / 2f;

                meshuv.AddRange(new List<Vector2>() { bottomleft + new Vector2(0, 0.5f), bottomleft + new Vector2(0.5f, 0.5f), bottomleft + new Vector2(0.5f, 0) });
            }

            // Generate faces
            VoxelTypes neighbourType;
            for (int x = 1; x < _dimentions.x - 1; x++)
            {
                for (int y = 1; y < _dimentions.y - 1; y++)
                {
                    for (int z = 1; z < _dimentions.z - 1; z++)
                    {
                        switch (_voxels[x, y, z]._type)
                        {
                            case VoxelTypes.Whole:
                            default:
                                for (int o = 0; o < 6; o++)
                                {
                                    if (_voxels[x + Faces[o, 4], y + Faces[o, 5], z + Faces[o, 6]]._type == 0)
                                    {
                                        AddQuad(o, meshVerticies.Count, x, y, z);
                                    }
                                }
                                break;
                            case VoxelTypes.SlopeForward:
                                AddQuad(6, meshVerticies.Count, x, y, z);
                                neighbourType = _voxels[x + Faces[4, 4], y + Faces[4, 5], z + Faces[4, 6]]._type;
                                if (neighbourType == VoxelTypes.Whole || neighbourType == VoxelTypes.SlopeRight)
                                {
                                    AddTri(4, meshVerticies.Count, x, y, z, false, true);
                                }
                                else if (neighbourType == VoxelTypes.SlopeBack || neighbourType == VoxelTypes.SlopeForwardInv)
                                {
                                    AddTri(30, meshVerticies.Count, x, y, z, true, true);
                                }
                                neighbourType = _voxels[x + Faces[5, 4], y + Faces[5, 5], z + Faces[5, 6]]._type;
                                if (neighbourType == VoxelTypes.Whole || neighbourType == VoxelTypes.SlopeLeft)
                                {
                                    AddTri(31, meshVerticies.Count, x, y, z, false, true);
                                }
                                else if (neighbourType == VoxelTypes.SlopeBack || neighbourType == VoxelTypes.SlopeForwardInv)
                                {
                                    AddTri(5, meshVerticies.Count, x, y, z, true, true);
                                }
                                break;
                            case VoxelTypes.SlopeRight:
                                AddQuad(7, meshVerticies.Count, x, y, z);
                                neighbourType = _voxels[x + Faces[2, 4], y + Faces[2, 5], z + Faces[2, 6]]._type;
                                if (neighbourType == VoxelTypes.Whole || neighbourType == VoxelTypes.SlopeForward)
                                {
                                    AddTri(28, meshVerticies.Count, x, y, z, false, true);
                                }
                                else if (neighbourType == VoxelTypes.SlopeLeft || neighbourType == VoxelTypes.CornerForwardRight || neighbourType == VoxelTypes.Empty || neighbourType == VoxelTypes.SlopeRightInv)
                                {
                                    AddTri(28, meshVerticies.Count, x, y, z, true, false);
                                }
                                neighbourType = _voxels[x + Faces[3, 4], y + Faces[3, 5], z + Faces[3, 6]]._type;
                                if (neighbourType == VoxelTypes.Whole || neighbourType == VoxelTypes.SlopeBack)
                                {
                                    AddTri(3, meshVerticies.Count, x, y, z, false, true);
                                }
                                else if (neighbourType == VoxelTypes.SlopeLeft || neighbourType == VoxelTypes.CornerBackLeft || neighbourType == VoxelTypes.Empty || neighbourType == VoxelTypes.SlopeRightInv)
                                {
                                    AddTri(3, meshVerticies.Count, x, y, z, true, false);
                                }
                                break;
                            case VoxelTypes.SlopeBack:
                                AddQuad(8, meshVerticies.Count, x, y, z);
                                neighbourType = _voxels[x + Faces[4, 4], y + Faces[4, 5], z + Faces[4, 6]]._type;
                                if (neighbourType == VoxelTypes.Whole || neighbourType == VoxelTypes.SlopeRight)
                                {
                                    AddTri(30, meshVerticies.Count, x, y, z, false, true);
                                }
                                else if (neighbourType == VoxelTypes.SlopeForward || neighbourType == VoxelTypes.SlopeBackInv)
                                {
                                    AddTri(4, meshVerticies.Count, x, y, z, true, true);
                                }
                                neighbourType = _voxels[x + Faces[5, 4], y + Faces[5, 5], z + Faces[5, 6]]._type;
                                if (neighbourType == VoxelTypes.Whole || neighbourType == VoxelTypes.SlopeLeft)
                                {
                                    AddTri(5, meshVerticies.Count, x, y, z, false, true);
                                }
                                else if (neighbourType == VoxelTypes.SlopeForward || neighbourType == VoxelTypes.SlopeBackInv)
                                {
                                    AddTri(31, meshVerticies.Count, x, y, z, true, true);
                                }
                                break;
                            case VoxelTypes.SlopeLeft:
                                AddQuad(9, meshVerticies.Count, x, y, z);
                                neighbourType = _voxels[x + Faces[2, 4], y + Faces[2, 5], z + Faces[2, 6]]._type;
                                if (neighbourType == VoxelTypes.Whole || neighbourType == VoxelTypes.SlopeForward)
                                {
                                    AddTri(2, meshVerticies.Count, x, y, z, false, true);
                                }
                                else if (neighbourType == VoxelTypes.SlopeRight || neighbourType == VoxelTypes.CornerForwardLeft || neighbourType == VoxelTypes.SlopeLeftInv)
                                {
                                    AddTri(2, meshVerticies.Count, x, y, z, true, false);
                                }
                                neighbourType = _voxels[x + Faces[3, 4], y + Faces[3, 5], z + Faces[3, 6]]._type;
                                if (neighbourType == VoxelTypes.Whole || neighbourType == VoxelTypes.SlopeBack)
                                {
                                    AddTri(29, meshVerticies.Count, x, y, z, false, true);
                                }
                                else if (neighbourType == VoxelTypes.SlopeRight || neighbourType == VoxelTypes.CornerBackRight || neighbourType == VoxelTypes.SlopeLeftInv)
                                {
                                    AddTri(29, meshVerticies.Count, x, y, z, true, false);
                                }
                                break;
                            case VoxelTypes.SlopeForwardInv:
                                AddQuad(10, meshVerticies.Count, x, y, z);
                                neighbourType = _voxels[x + Faces[4, 4], y + Faces[4, 5], z + Faces[4, 6]]._type;
                                if (neighbourType == VoxelTypes.Whole || neighbourType == VoxelTypes.SlopeRightInv)
                                {
                                    AddTri(30, meshVerticies.Count, x, y, z, true, true);
                                }
                                else if (neighbourType == VoxelTypes.SlopeBackInv)
                                {
                                    AddTri(30, meshVerticies.Count, x, y, z, false, false);
                                }
                                neighbourType = _voxels[x + Faces[5, 4], y + Faces[5, 5], z + Faces[5, 6]]._type;
                                if (neighbourType == VoxelTypes.Whole || neighbourType == VoxelTypes.SlopeLeftInv)
                                {
                                    AddTri(5, meshVerticies.Count, x, y, z, true, true);
                                }
                                else if (neighbourType == VoxelTypes.SlopeBackInv || neighbourType == VoxelTypes.SlopeForward)
                                {
                                    AddTri(5, meshVerticies.Count, x, y, z, false, false);
                                }
                                break;
                            case VoxelTypes.SlopeRightInv:
                                AddQuad(11, meshVerticies.Count, x, y, z);
                                neighbourType = _voxels[x + Faces[2, 4], y + Faces[2, 5], z + Faces[2, 6]]._type;
                                if (neighbourType == VoxelTypes.Whole || neighbourType == VoxelTypes.SlopeForwardInv)
                                {
                                    AddTri(2, meshVerticies.Count, x, y, z, true, true);
                                }
                                else if (neighbourType == VoxelTypes.SlopeLeftInv || neighbourType == VoxelTypes.CornerForwardRightInv || neighbourType == VoxelTypes.Empty)
                                {
                                    AddTri(2, meshVerticies.Count, x, y, z, false, false);
                                }
                                neighbourType = _voxels[x + Faces[3, 4], y + Faces[3, 5], z + Faces[3, 6]]._type;
                                if (neighbourType == VoxelTypes.Whole || neighbourType == VoxelTypes.SlopeBackInv)
                                {
                                    AddTri(29, meshVerticies.Count, x, y, z, true, true);
                                }
                                else if (neighbourType == VoxelTypes.SlopeLeftInv || neighbourType == VoxelTypes.CornerBackLeftInv || neighbourType == VoxelTypes.Empty)
                                {
                                    AddTri(29, meshVerticies.Count, x, y, z, false, false);
                                }
                                break;
                            case VoxelTypes.SlopeBackInv:
                                AddQuad(12, meshVerticies.Count, x, y, z);
                                neighbourType = _voxels[x + Faces[4, 4], y + Faces[4, 5], z + Faces[4, 6]]._type;
                                if (neighbourType == VoxelTypes.Whole || neighbourType == VoxelTypes.SlopeRightInv)
                                {
                                    AddTri(4, meshVerticies.Count, x, y, z, true, true);
                                }
                                else if (neighbourType == VoxelTypes.SlopeForwardInv)
                                {
                                    AddTri(4, meshVerticies.Count, x, y, z, false, false);
                                }
                                neighbourType = _voxels[x + Faces[5, 4], y + Faces[5, 5], z + Faces[5, 6]]._type;
                                if (neighbourType == VoxelTypes.Whole || neighbourType == VoxelTypes.SlopeLeftInv)
                                {
                                    AddTri(31, meshVerticies.Count, x, y, z, true, true);
                                }
                                else if (neighbourType == VoxelTypes.SlopeForwardInv)
                                {
                                    AddTri(31, meshVerticies.Count, x, y, z, false, false);
                                }
                                break;
                            case VoxelTypes.SlopeLeftInv:
                                AddQuad(13, meshVerticies.Count, x, y, z);
                                neighbourType = _voxels[x + Faces[2, 4], y + Faces[2, 5], z + Faces[2, 6]]._type;
                                if (neighbourType == VoxelTypes.Whole || neighbourType == VoxelTypes.SlopeForwardInv)
                                {
                                    AddTri(28, meshVerticies.Count, x, y, z, true, true);
                                }
                                else if (neighbourType == VoxelTypes.SlopeRightInv)
                                {
                                    AddTri(28, meshVerticies.Count, x, y, z, false, false);
                                }
                                neighbourType = _voxels[x + Faces[3, 4], y + Faces[3, 5], z + Faces[3, 6]]._type;
                                if (neighbourType == VoxelTypes.Whole || neighbourType == VoxelTypes.SlopeBackInv)
                                {
                                    AddTri(3, meshVerticies.Count, x, y, z, true, true);
                                }
                                else if (neighbourType == VoxelTypes.SlopeRightInv)
                                {
                                    AddTri(3, meshVerticies.Count, x, y, z, false, false);
                                }
                                break;
                            case VoxelTypes.CornerForwardRight:
                                AddTri(14, meshVerticies.Count, x, y, z);
                                neighbourType = _voxels[x + Faces[3, 4], y + Faces[3, 5], z + Faces[3, 6]]._type;
                                if (neighbourType == VoxelTypes.Whole || neighbourType == VoxelTypes.SlopeBack)
                                {
                                    AddTri(29, meshVerticies.Count, x, y, z, false, true);
                                }
                                neighbourType = _voxels[x + Faces[4, 4], y + Faces[4, 5], z + Faces[4, 6]]._type;
                                if (neighbourType == VoxelTypes.Whole || neighbourType == VoxelTypes.SlopeRight)
                                {
                                    AddTri(4, meshVerticies.Count, x, y, z, false, true);
                                }
                                neighbourType = _voxels[x + Faces[1, 4], y + Faces[1, 5], z + Faces[1, 6]]._type;
                                if (neighbourType == VoxelTypes.Whole || neighbourType == VoxelTypes.SlopeForwardInv)
                                {
                                    AddTri(27, meshVerticies.Count, x, y, z, true, true);
                                }
                                break;
                            case VoxelTypes.CornerBackRight:
                                AddTri(15, meshVerticies.Count, x, y, z);
                                neighbourType = _voxels[x + Faces[2, 4], y + Faces[2, 5], z + Faces[2, 6]]._type;
                                if (neighbourType == VoxelTypes.Whole || neighbourType == VoxelTypes.SlopeForward || neighbourType == VoxelTypes.SlopeHorizontalBack)
                                {
                                    AddTri(28, meshVerticies.Count, x, y, z, false, true);
                                }
                                neighbourType = _voxels[x + Faces[5, 4], y + Faces[5, 5], z + Faces[5, 6]]._type;
                                if (neighbourType == VoxelTypes.Whole || neighbourType == VoxelTypes.SlopeLeft || neighbourType == VoxelTypes.SlopeHorizontalForward)
                                {
                                    AddTri(5, meshVerticies.Count, x, y, z, false, true);
                                }
                                neighbourType = _voxels[x + Faces[1, 4], y + Faces[1, 5], z + Faces[1, 6]]._type;
                                if (neighbourType == VoxelTypes.Whole || neighbourType == VoxelTypes.SlopeBackInv)
                                {
                                    AddTri(27, meshVerticies.Count, x, y, z, false, true);
                                }
                                break;
                            case VoxelTypes.CornerBackLeft:
                                AddTri(16, meshVerticies.Count, x, y, z);
                                if (_voxels[x + Faces[2, 4], y + Faces[2, 5], z + Faces[2, 6]]._type == VoxelTypes.Whole)
                                {
                                    AddTri(2, meshVerticies.Count, x, y, z, false, true);
                                }
                                if (_voxels[x + Faces[4, 4], y + Faces[4, 5], z + Faces[4, 6]]._type == VoxelTypes.Whole)
                                {
                                    AddTri(30, meshVerticies.Count, x, y, z, false, true);
                                }
                                if (_voxels[x + Faces[1, 4], y + Faces[1, 5], z + Faces[1, 6]]._type == VoxelTypes.Whole)
                                {
                                    AddTri(1, meshVerticies.Count, x, y, z, true, true);
                                }
                                break;
                            case VoxelTypes.CornerForwardLeft:
                                AddTri(17, meshVerticies.Count, x, y, z);
                                if (_voxels[x + Faces[3, 4], y + Faces[3, 5], z + Faces[3, 6]]._type == VoxelTypes.Whole)
                                {
                                    AddTri(3, meshVerticies.Count, x, y, z, false, true);
                                }
                                if (_voxels[x + Faces[5, 4], y + Faces[5, 5], z + Faces[5, 6]]._type == VoxelTypes.Whole)
                                {
                                    AddTri(31, meshVerticies.Count, x, y, z, false, true);
                                }
                                if (_voxels[x + Faces[1, 4], y + Faces[1, 5], z + Faces[1, 6]]._type == VoxelTypes.Whole)
                                {
                                    AddTri(1, meshVerticies.Count, x, y, z, false, true);
                                }
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
                                if (_voxels[x + Faces[0, 4], y + Faces[0, 5], z + Faces[0, 6]]._type == VoxelTypes.Whole)
                                {
                                    AddTri(26, meshVerticies.Count, x, y, z, false, true);
                                }
                                if (_voxels[x + Faces[1, 4], y + Faces[1, 5], z + Faces[1, 6]]._type == VoxelTypes.Whole)
                                {
                                    AddTri(1, meshVerticies.Count, x, y, z, true, true);
                                }
                                break;
                            case VoxelTypes.SlopeHorizontalRight:
                                AddQuad(23, meshVerticies.Count, x, y, z);
                                if (_voxels[x + Faces[0, 4], y + Faces[0, 5], z + Faces[0, 6]]._type == VoxelTypes.Whole)
                                {
                                    AddTri(0, meshVerticies.Count, x, y, z, true, true);
                                }
                                if (_voxels[x + Faces[1, 4], y + Faces[1, 5], z + Faces[1, 6]]._type == VoxelTypes.Whole)
                                {
                                    AddTri(27, meshVerticies.Count, x, y, z, false, true);
                                }
                                break;
                            case VoxelTypes.SlopeHorizontalBack:
                                AddQuad(24, meshVerticies.Count, x, y, z);
                                if (_voxels[x + Faces[0, 4], y + Faces[0, 5], z + Faces[0, 6]]._type == VoxelTypes.Whole)
                                {
                                    AddTri(26, meshVerticies.Count, x, y, z, true, true);
                                }
                                if (_voxels[x + Faces[1, 4], y + Faces[1, 5], z + Faces[1, 6]]._type == VoxelTypes.Whole)
                                {
                                    AddTri(1, meshVerticies.Count, x, y, z, false, true);
                                }
                                break;
                            case VoxelTypes.SlopeHorizontalLeft:
                                AddQuad(25, meshVerticies.Count, x, y, z);
                                if (_voxels[x + Faces[0, 4], y + Faces[0, 5], z + Faces[0, 6]]._type == VoxelTypes.Whole)
                                {
                                    AddTri(0, meshVerticies.Count, x, y, z, false, true);
                                }
                                if (_voxels[x + Faces[1, 4], y + Faces[1, 5], z + Faces[1, 6]]._type == VoxelTypes.Whole)
                                {
                                    AddTri(27, meshVerticies.Count, x, y, z, true, true);
                                }
                                break;
                            case VoxelTypes.Empty:
                                break;
                        }
                    }
                }
            }
            // Merge collision mesh along X axis
            VoxelTypes lastType = VoxelTypes.Empty;
            List<int> vertsToBeDeleted = new List<int>();
            int vertsToBeReplaced = int.MaxValue;
            for (int z = _dimentions.z - 2; z != 0; z--)
            {
                for (int y = _dimentions.y - 2; y != 0; y--)
                {
                    for (int x = _dimentions.x - 1; x != 0; x--)
                    {
                        if (vertsToBeReplaced == int.MaxValue)
                        {
                            vertsToBeReplaced = triOffsets[x, y, z];
                            lastType = _voxels[x, y, z]._type;
                        }
                        else if (lastType != _voxels[x, y, z]._type)
                        {
                            // Merge previous verts
                            if (vertsToBeDeleted.Count != 0)
                            {
                                print(meshTrianglesCollision[vertsToBeReplaced]);
                                print("Count: " + meshVerticiesCollision.Count);
                                meshVerticiesCollision[meshTrianglesCollision[vertsToBeReplaced]] = Vector3.zero;
                                meshVerticiesCollision[meshTrianglesCollision[vertsToBeReplaced]] = meshVerticiesCollision[meshTrianglesCollision[vertsToBeDeleted[vertsToBeDeleted.Count - 1]]];
                                meshVerticiesCollision[meshTrianglesCollision[vertsToBeReplaced + 1]] = meshVerticiesCollision[meshTrianglesCollision[vertsToBeDeleted[vertsToBeDeleted.Count - 1] + 1]];
                                meshVerticiesCollision[meshTrianglesCollision[vertsToBeReplaced + 3]] = meshVerticiesCollision[meshTrianglesCollision[vertsToBeDeleted[vertsToBeDeleted.Count - 1] + 3]];
                            }
                            foreach (int vert in vertsToBeDeleted)
                            {
                                meshVerticiesCollision.RemoveRange(meshTrianglesCollision[vert], 6);
                                meshTrianglesCollision.RemoveRange(vert, 6);
                            }
                            // Start tracking new verts
                            vertsToBeDeleted.Clear();
                            vertsToBeReplaced = triOffsets[x, y, z];
                            lastType = _voxels[x, y, z]._type;
                        }
                        else
                        {
                            vertsToBeDeleted.Add(triOffsets[x, y, z]);
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
            _meshCollision = new Mesh()
            {
                vertices = meshVerticiesCollision.ToArray(),
                triangles = meshTrianglesCollision.ToArray(),
                uv = meshuvCollision.ToArray()
            };
            _meshCollision.RecalculateBounds();
            gameObject.GetComponent<MeshCollider>().sharedMesh = _meshCollision;
        }

        // Sets each voxels' type
        void MeshMarch()
        {
            for (int x = 1; x < _dimentions.x - 1; ++x)
            {
                for (int y = 1; y < _dimentions.y - 1; ++y)
                {
                    for (int z = 1; z < _dimentions.y - 1; ++z)
                    {
                        if (_voxels[x, y, z]._type != VoxelTypes.Empty)
                        {
                            BlockMarch(x, y, z);
                        }
                    }
                }
            }
        }
        void BlockMarch(int x, int y, int z)
        {
            int voxelValue = 0;
            if (_voxels[x + 1, y, z]._type != VoxelTypes.Empty)
            {
                voxelValue = voxelValue | 1;
            }
            if (_voxels[x - 1, y, z]._type != VoxelTypes.Empty)
            {
                voxelValue = voxelValue | 2;
            }
            if (_voxels[x, y + 1, z]._type != VoxelTypes.Empty)
            {
                voxelValue = voxelValue | 4;
            }
            if (_voxels[x, y - 1, z]._type != VoxelTypes.Empty)
            {
                voxelValue = voxelValue | 8;
            }
            if (_voxels[x, y, z + 1]._type != VoxelTypes.Empty)
            {
                voxelValue = voxelValue | 16;
            }
            if (_voxels[x, y, z - 1]._type != VoxelTypes.Empty)
            {
                voxelValue = voxelValue | 32;
            }

            switch (voxelValue)
            {
                default:
                    _voxels[x, y, z]._type = VoxelTypes.Whole;
                    _voxels[x, y, z]._occupiedVerts = 0b000001 + 0b000001 << 3 + 0b000001 << 6 + 0b000001 << 9 + 0b000001 << 12 + 0b000001 << 15;
                    break;
                case 5:
                case 53:
                    _voxels[x, y, z]._type = VoxelTypes.SlopeLeftInv;
                    _voxels[x, y, z]._occupiedVerts = 0b000001 + 0b000010 << 6 + 0b000100 << 9 + 0b000001 << 12;
                    break;
                case 6:
                case 54:
                    _voxels[x, y, z]._type = VoxelTypes.SlopeRightInv;
                    break;
                case 9:
                case 57:
                    _voxels[x, y, z]._type = VoxelTypes.SlopeLeft;
                    break;
                case 10:
                case 58:
                    _voxels[x, y, z]._type = VoxelTypes.SlopeRight;
                    break;
                case 20:
                case 36:
                case 39:
                    _voxels[x, y, z]._type = VoxelTypes.SlopeForwardInv;
                    break;
                case 21:
                    _voxels[x, y, z]._type = VoxelTypes.CornerForwardRightInv;
                    break;
                case 22:
                    _voxels[x, y, z]._type = VoxelTypes.CornerBackRightInv;
                    break;
                case 23:
                case 24:
                    _voxels[x, y, z]._type = VoxelTypes.SlopeBackInv;
                    break;
                case 25:
                    _voxels[x, y, z]._type = VoxelTypes.CornerBackLeft;
                    break;
                case 38:
                    _voxels[x, y, z]._type = VoxelTypes.CornerBackLeftInv;
                    break;
                case 26:
                    _voxels[x, y, z]._type = VoxelTypes.CornerBackRight;
                    break;
                case 27:
                    _voxels[x, y, z]._type = VoxelTypes.SlopeBack;
                    break;
                case 29:
                    _voxels[x, y, z]._type = VoxelTypes.SlopeHorizontalForward;
                    break;
                case 30:
                    _voxels[x, y, z]._type = VoxelTypes.SlopeHorizontalRight;
                    break;
                case 37:
                    _voxels[x, y, z]._type = VoxelTypes.CornerForwardLeftInv;
                    break;
                case 40:
                case 43:
                    _voxels[x, y, z]._type = VoxelTypes.SlopeForward;
                    break;
                case 41:
                    _voxels[x, y, z]._type = VoxelTypes.CornerForwardRight;
                    break;
                case 42:
                    _voxels[x, y, z]._type = VoxelTypes.CornerForwardLeft;
                    break;
                case 45:
                    _voxels[x, y, z]._type = VoxelTypes.SlopeHorizontalLeft;
                    break;
                case 46:
                    _voxels[x, y, z]._type = VoxelTypes.SlopeHorizontalBack;
                    break;
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
