using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System; // Math.Clamp

namespace Voxel
{
    public class VoxelMesh : MonoBehaviour
    {
        [SerializeField]
        private Vector3Int _dimentions = new Vector3Int(10, 10, 10);
        private Mesh _meshCollision;
        [SerializeField]
        private bool _fillGrid = false;
        public GameObject _prefab;
        [SerializeField]
        private uint _blockCount = 0;

        // Bounds
        private int boundsMinX = 1;
        private int boundsMaxX = 1;
        private int boundsMinY = 1;
        private int boundsMaxY = 1;
        private int boundsMinZ = 1;
        private int boundsMaxZ = 1;

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
            // Decides what face to attempt to render for each side
            // 0b000000 = Empty
            // 0b000001 = Full
            // 0b000010 = Half Type 1 (0,1,2)
            // 0b000011 = Half Type 2 (0,2,3)
            // 0b000100 = Half Type 3 (1,2,3)
            // 0b000101 = Half Type 4 (1,3,0)
            // Top, Bottom, Right, Left, Forward, Back
            public uint _occupiedFaces = 0;
            public int _quadType = 0;
            public bool _isTri = false;
            public bool _otherTri = false;
        }
        private Voxel[,,] _voxels;

        public Texture2D _texture;

        private void Awake()
        {
            _voxels = new Voxel[_dimentions.x, _dimentions.y, _dimentions.z];
            _texture = new Texture2D(_dimentions.x, _dimentions.y/* * _dimentions.z*/, TextureFormat.ARGB32, false);
            _texture.filterMode = FilterMode.Point;
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
            _texture.Apply();
            GetComponent<MeshRenderer>().material.mainTexture = _texture;

            if (_fillGrid)
            {
                FillGrid();
                MeshMarch();
                GenerateMesh();
            }
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
            else if (Input.GetMouseButtonDown(2))
            {
                RaycastHit hit;
                if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit))
                {
                    if (hit.collider.gameObject == gameObject)
                    {
                        PaintVoxel(transform.InverseTransformPoint(hit.point), Color.black);
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.R))
                CenterGrid();
            if (Input.GetKeyDown(KeyCode.T))
                ResizeGrid(new Vector3Int(_dimentions.x + 1, _dimentions.y + 1, _dimentions.z + 1));
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
                        ++_blockCount;
                        UpdateBounds(new Vector3Int(x, y, z));
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
            selectedVoxel = (VoxelTypes)Mathf.Min((int)selectedVoxel, 1);
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
                    selectedVoxel = (VoxelTypes)Mathf.Min((int)selectedVoxel, 1);
                    if (selectedVoxel == newValue)
                    {
                        // Targeted shape is likely a slope or a corner, target adjacent space
                        Vector3 direction = transform.TransformPoint(point) - Camera.main.transform.position;
                        //print(transform.TransformPoint(point) + " - " + Camera.main.transform.position + " = " + direction);
                        Vector3Int dir;
                        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
                        {
                            //print("AAA");
                            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.z))
                                dir = new Vector3Int((int)Math.Clamp(-direction.x, -1, 1), 0, 0);
                            else
                                dir = new Vector3Int(0, 0, (int)Math.Clamp(-direction.z, -1, 1));
                        }
                        else if (Mathf.Abs(direction.y) > Mathf.Abs(direction.z))
                            dir = new Vector3Int(0, (int)Math.Clamp(-direction.y, -1, 1), 0);
                        else
                            dir = new Vector3Int(0, 0, (int)Math.Clamp(-direction.z, -1, 1));
                        print(dir);
                        alternativeVoxel += dir;
                        alternativeVoxel = Vector3Int.Max(new Vector3Int(1, 1, 1), alternativeVoxel);
                        alternativeVoxel = Vector3Int.Min(new Vector3Int(_dimentions.x - 2, _dimentions.y - 2, _dimentions.z - 2), alternativeVoxel);
                    }

                    print("(" + alternativeVoxel.x + ", " + alternativeVoxel.y + ", " + alternativeVoxel.z + ") = " + _voxels[alternativeVoxel.x, alternativeVoxel.y, alternativeVoxel.z]._type + " (Alt)");
                    _voxels[alternativeVoxel.x, alternativeVoxel.y, alternativeVoxel.z]._type = newValue;
                    if (newValue == VoxelTypes.Empty)
                    {
                        CheckGridConnection(alternativeVoxel);
                        --_blockCount;
                    }
                    else
                        ++_blockCount;
                    UpdateBounds(new Vector3Int(alternativeVoxel.x, alternativeVoxel.y, alternativeVoxel.z));

                    for (int x = Mathf.Max(1, alternativeVoxel.x - 1); x <= Mathf.Min(_dimentions.x - 2, alternativeVoxel.x + 1); ++x)
                    {
                        for (int y = Mathf.Max(1, alternativeVoxel.y - 1); y <= Mathf.Min(_dimentions.y - 2, alternativeVoxel.y + 1); ++y)
                        {
                            for (int z = Mathf.Max(1, alternativeVoxel.z - 1); z <= Mathf.Min(_dimentions.z - 2, alternativeVoxel.z + 1); ++z)
                            {
                                if (_voxels[x, y, z]._type != VoxelTypes.Empty)
                                {
                                    BlockMarch(x, y, z);
                                }
                                else
                                {
                                    _voxels[x, y, z]._occupiedFaces = 0;
                                    _voxels[x, y, z]._quadType = 0;
                                    _voxels[x, y, z]._isTri = false;
                                    _voxels[x, y, z]._otherTri = false;
                                }
                            }
                        }
                    }
                    GenerateMesh();
                }
            }
            else if (voxelX != 0 && voxelX != _dimentions.x - 1 &&
                    voxelY != 0 && voxelY != _dimentions.y - 1 &&
                    voxelZ != 0 && voxelZ != _dimentions.z - 1)
            {
                print("(" + voxelX + ", " + voxelY + ", " + voxelZ + ") = " + _voxels[voxelX, voxelY, voxelZ]._type);
                _voxels[voxelX, voxelY, voxelZ]._type = newValue;
                if (newValue == VoxelTypes.Empty)
                {
                    CheckGridConnection(new Vector3Int(voxelX, voxelY, voxelZ));
                    --_blockCount;
                }
                else
                    ++_blockCount;
                UpdateBounds(new Vector3Int(voxelX, voxelY, voxelZ));

                for (int x = Mathf.Max(1, voxelX - 1); x <= Mathf.Min(_dimentions.x - 2, voxelX + 1); ++x)
                {
                    for (int y = Mathf.Max(1, voxelY - 1); y <= Mathf.Min(_dimentions.y - 2, voxelY + 1); ++y)
                    {
                        for (int z = Mathf.Max(1, voxelZ - 1); z <= Mathf.Min(_dimentions.z - 2, voxelZ + 1); ++z)
                        {
                            if (_voxels[x, y, z]._type != VoxelTypes.Empty)
                            {
                                BlockMarch(x, y, z);
                            }
                            else
                            {
                                _voxels[x, y, z]._occupiedFaces = 0;
                                _voxels[x, y, z]._quadType = 0;
                                _voxels[x, y, z]._isTri = false;
                                _voxels[x, y, z]._otherTri = false;
                            }
                        }
                    }
                }
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
            {0,1,2,3,0,1,0,0,0},            // Top [0]
            {7,6,5,4,0,-1,0,0,0},           // Bottom
            {2,1,5,6,0,0,1,0,0},            // Front
            {0,3,7,4,0,0,-1,0,0},           // Back
            {3,2,6,7,1,0,0,0,0},            // Right
            {1,0,4,5,-1,0,0,0,0},           // Left
            {3,0,5,6,0,0,0,0,0},            // Slope Forward [6]
            {0,1,6,7,0,0,0,0,0},            // Slope Right
            {1,2,7,4,0,0,0,0,0},            // Slope Back
            {2,3,4,5,0,0,0,0,0},            // Slope Left
            {2,1,4,7,0,0,0,0,0},            // Slope Forward Inverted
            {3,2,5,4,0,0,0,0,0},            // Slope Right Inverted
            {0,3,6,5,0,0,0,0,0},            // Slope Back Inverted
            {1,0,7,6,0,0,0,0,0},            // Slope Left Inverted
            {3,4,6,0,0,0,0,0,0},            // Corner Forward Right [14]
            {1,6,4,0,0,0,0,0,0},            // Corner Back Right
            {5,2,7,4,0,0,0,0,0},            // Corner Back Left
            {0,5,7,0,0,0,0,0,0},            // Corner Forward Left
            {6,1,3,0,0,0,0,0,0},            // Corner Forward Right Inverted
            {5,0,2,0,0,0,0,0,0},            // Corner Back Right Inverted
            {4,3,1,0,0,0,0,0,0},            // Corner Back Left Inverted
            {7,2,0,0,0,0,0,0,0},            // Corner Forward Left Inverted
            {1,3,7,5,0,0,0,0,0},            // Slope Horizontal Forward [22]
            {0,2,6,4,0,0,0,0,0},            // Slope Horizontal Right
            {3,1,5,7,0,0,0,0,0},            // Slope Horizontal Down
            {2,0,4,6,0,0,0,0,0},            // Slope Horizontal Left
            {3,0,1,2,0,1,0,0,0},            // Top Alternate [26]
            {4,7,6,5,0,-1,0,0,0},           // Bottom Alternate
            {6,2,1,5,0,0,1,0,0},            // Front Alternate
            {4,0,3,7,0,0,-1,0,0},           // Back Alternate
            {7,3,2,6,1,0,0,0,0},            // Right Alternate
            {5,1,0,4,-1,0,0,0,0}            // Left Alternate
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
                //meshuv.AddRange(new List<Vector2>() { bottomleft + new Vector2(0, 0.5f), bottomleft + new Vector2(0.5f, 0.5f), bottomleft + new Vector2(0.5f, 0), bottomleft });
                meshuv.AddRange(new List<Vector2>() { new Vector2((float)x / _texture.width, (float)(y/* + y * z + 1*/)/_texture.height),
                    new Vector2((float)(x + 1) / _texture.width, (float)(y/* + y * z + 1*/) / _texture.height), 
                    new Vector2((float)(x + 1) / _texture.width, (float)(y/* + y * z*/)/_texture.height),
                    new Vector2((float)(x)/_texture.width, (float)(y/* + y * z*/)/_texture.height) });

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

                //meshuv.AddRange(new List<Vector2>() { bottomleft + new Vector2(0, 0.5f), bottomleft + new Vector2(0.5f, 0.5f), bottomleft + new Vector2(0.5f, 0) });
                meshuv.AddRange(new List<Vector2>() { new Vector2((float)x / _texture.width, (float)(y/* + y * z + 1*/)/_texture.height),
                    new Vector2((float)(x + 1) / _texture.width, (float)(y/* + y * z + 1*/) / _texture.height),
                    new Vector2((float)(x + 1) / _texture.width, (float)(y/* + y * z*/)/_texture.height)});
            }

            // Generate faces
            for (int x = 1; x < _dimentions.x - 1; x++)
            {
                for (int y = 1; y < _dimentions.y - 1; y++)
                {
                    for (int z = 1; z < _dimentions.z - 1; z++)
                    {
                        Voxel voxel = _voxels[x, y, z];
                        // Draw shape
                        if (voxel._isTri)
                        {
                            AddTri(voxel._quadType, meshVerticies.Count, x, y, z, voxel._otherTri);
                        }
                        else if (voxel._quadType != 0)
                        {
                            AddQuad(voxel._quadType, meshVerticies.Count, x, y, z);
                        }
                        // Fill Gaps
                        for (int i = 0; i < 6; ++i)
                        {
                            uint faceMask = voxel._occupiedFaces >> i * 3;
                            faceMask = faceMask & 0b000111;
                            uint opposingFaceMask = _voxels[x + Faces[i, 4], y + Faces[i, 5], z + Faces[i, 6]]._occupiedFaces >> (i % 2 == 0 ? i + 1 : i - 1) * 3;
                            opposingFaceMask = opposingFaceMask & 0b000111;

                            if (faceMask == 0b000001 && opposingFaceMask == 0b000000)
                            {
                                AddQuad(i, meshVerticies.Count, x, y, z);
                            }
                            else if (faceMask == 0b000010 && (opposingFaceMask == 0b000011 || opposingFaceMask == 0b000000 || opposingFaceMask == 0b000010 || opposingFaceMask == 0b000011))
                            {
                                AddTri(i, meshVerticies.Count, x, y, z);
                            }
                            else if (faceMask == 0b000011 && (opposingFaceMask == 0b000010 || opposingFaceMask == 0b000000 || opposingFaceMask == 0b000010 || opposingFaceMask == 0b000011))
                            {
                                AddTri(i, meshVerticies.Count, x, y, z, true);
                            }
                            else if (faceMask == 0b000100 && (opposingFaceMask == 0b000101 || opposingFaceMask == 0b000000 || opposingFaceMask == 0b000100 || opposingFaceMask == 0b000101))
                            {
                                AddTri(i + 26, meshVerticies.Count, x, y, z);
                            }
                            else if (faceMask == 0b000101 && (opposingFaceMask == 0b000100 || opposingFaceMask == 0b000000 || opposingFaceMask == 0b000100 || opposingFaceMask == 0b000101))
                            {
                                AddTri(i + 26, meshVerticies.Count, x, y, z, true);
                            }

                            if (faceMask == 0b000001)
                            {
                                if (opposingFaceMask == 0b000011)
                                {
                                    if (i == 0 || i == 1 || i == 4)
                                        AddTri(i + 26, meshVerticies.Count, x, y, z, true);
                                    else
                                        AddTri(i + 26, meshVerticies.Count, x, y, z);
                                }
                                else if (opposingFaceMask == 0b000010 || opposingFaceMask == 0b000011)
                                {
                                    if (i == 2 || i == 3 || i == 5)
                                        AddTri(i + 26, meshVerticies.Count, x, y, z, true);
                                    else
                                        AddTri(i + 26, meshVerticies.Count, x, y, z);
                                }
                                else if (opposingFaceMask == 0b000101)
                                {
                                    if (i == 0 || i == 1)
                                        AddTri(i, meshVerticies.Count, x, y, z, true);
                                    else
                                        AddTri(i, meshVerticies.Count, x, y, z);
                                }
                                else if (opposingFaceMask == 0b000100)
                                {
                                    if (i == 0 || i == 1)
                                        AddTri(i, meshVerticies.Count, x, y, z);
                                    else
                                        AddTri(i, meshVerticies.Count, x, y, z, true);
                                }
                            }
                        }
                    }
                }
            }

            // Merge collision mesh along X axis
            //VoxelTypes lastType = VoxelTypes.Empty;
            //List<int> vertsToBeDeleted = new List<int>();
            //int vertsToBeReplaced = int.MaxValue;
            //for (int z = _dimentions.z - 2; z != 0; z--)
            //{
            //    for (int y = _dimentions.y - 2; y != 0; y--)
            //    {
            //        for (int x = _dimentions.x - 1; x != 0; x--)
            //        {
            //            if (vertsToBeReplaced == int.MaxValue)
            //            {
            //                vertsToBeReplaced = triOffsets[x, y, z];
            //                lastType = _voxels[x, y, z]._type;
            //            }
            //            else if (lastType != _voxels[x, y, z]._type)
            //            {
            //                // Merge previous verts
            //                if (vertsToBeDeleted.Count != 0)
            //                {
            //                    print(meshTrianglesCollision[vertsToBeReplaced]);
            //                    print("Count: " + meshVerticiesCollision.Count);
            //                    meshVerticiesCollision[meshTrianglesCollision[vertsToBeReplaced]] = Vector3.zero;
            //                    meshVerticiesCollision[meshTrianglesCollision[vertsToBeReplaced]] = meshVerticiesCollision[meshTrianglesCollision[vertsToBeDeleted[vertsToBeDeleted.Count - 1]]];
            //                    meshVerticiesCollision[meshTrianglesCollision[vertsToBeReplaced + 1]] = meshVerticiesCollision[meshTrianglesCollision[vertsToBeDeleted[vertsToBeDeleted.Count - 1] + 1]];
            //                    meshVerticiesCollision[meshTrianglesCollision[vertsToBeReplaced + 3]] = meshVerticiesCollision[meshTrianglesCollision[vertsToBeDeleted[vertsToBeDeleted.Count - 1] + 3]];
            //                }
            //                foreach (int vert in vertsToBeDeleted)
            //                {
            //                    meshVerticiesCollision.RemoveRange(meshTrianglesCollision[vert], 6);
            //                    meshTrianglesCollision.RemoveRange(vert, 6);
            //                }
            //                // Start tracking new verts
            //                vertsToBeDeleted.Clear();
            //                vertsToBeReplaced = triOffsets[x, y, z];
            //                lastType = _voxels[x, y, z]._type;
            //            }
            //            else
            //            {
            //                vertsToBeDeleted.Add(triOffsets[x, y, z]);
            //            }
            //        }
            //    }
            //}

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
                        else
                        {
                            _voxels[x, y, z]._occupiedFaces = 0;
                            _voxels[x, y, z]._quadType = 0;
                            _voxels[x, y, z]._isTri = false;
                            _voxels[x, y, z]._otherTri = false;
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

            _voxels[x, y, z]._occupiedFaces = 0;
            _voxels[x, y, z]._quadType = 0;
            _voxels[x, y, z]._isTri = false;
            _voxels[x, y, z]._otherTri = false;
            switch (voxelValue)
            {
                default:
                    _voxels[x, y, z]._type = VoxelTypes.Whole;
                    _voxels[x, y, z]._occupiedFaces = 0b000001 + (0b000001 << 3) + (0b000001 << 6) + (0b000001 << 9) + (0b000001 << 12) + (0b000001 << 15);
                    break;
                case 5:
                case 53:
                    _voxels[x, y, z]._type = VoxelTypes.SlopeLeftInv;
                    _voxels[x, y, z]._occupiedFaces = 0b000001 + (0b000100 << 6) + (0b000010 << 9) + (0b000001 << 12);
                    _voxels[x, y, z]._quadType = 13;
                    break;
                case 6:
                case 54:
                    _voxels[x, y, z]._type = VoxelTypes.SlopeRightInv;
                    _voxels[x, y, z]._occupiedFaces = 0b000001 + (0b000010 << 6) + (0b0000100 << 9) + (0b000001 << 15);
                    _voxels[x, y, z]._quadType = 11;
                    break;
                case 9:
                case 57:
                    _voxels[x, y, z]._type = VoxelTypes.SlopeLeft;
                    _voxels[x, y, z]._occupiedFaces = (0b000001 << 3) + (0b000011 << 6) + (0b000101 << 9) + (0b000001 << 12);
                    _voxels[x, y, z]._quadType = 9;
                    break;
                case 10:
                case 58:
                    _voxels[x, y, z]._type = VoxelTypes.SlopeRight;
                    _voxels[x, y, z]._occupiedFaces = (0b000001 << 3) + (0b000101 << 6) + (0b000011 << 9) + (0b000001 << 15);
                    _voxels[x, y, z]._quadType = 7;
                    break;
                case 36:
                case 39:
                    _voxels[x, y, z]._type = VoxelTypes.SlopeForwardInv;
                    _voxels[x, y, z]._occupiedFaces = 0b000001 + (0b000001 << 9) + (0b000100 << 12) + (0b000011 << 15);
                    _voxels[x, y, z]._quadType = 10;
                    break;
                case 21:
                    _voxels[x, y, z]._type = VoxelTypes.CornerForwardRightInv;
                    _voxels[x, y, z]._occupiedFaces = 0b000101 + (0b000100 << 6) + (0b000010 << 12);
                    _voxels[x, y, z]._quadType = 18;
                    _voxels[x, y, z]._isTri = true;
                    break;
                case 22:
                    _voxels[x, y, z]._type = VoxelTypes.CornerBackRightInv;
                    _voxels[x, y, z]._occupiedFaces = 0b000010 + (0b000010 << 6) + (0b000100 << 15);
                    _voxels[x, y, z]._quadType = 19;
                    _voxels[x, y, z]._isTri = true;
                    break;
                case 20:
                case 23:
                    _voxels[x, y, z]._type = VoxelTypes.SlopeBackInv;
                    _voxels[x, y, z]._occupiedFaces = 0b000001 + (0b000001 << 6) + (0b000010 << 12) + (0b000100 << 15);
                    _voxels[x, y, z]._quadType = 12;
                    break;
                case 24:
                case 25:
                    _voxels[x, y, z]._type = VoxelTypes.CornerBackLeft;
                    _voxels[x, y, z]._occupiedFaces = (0b000010 << 3) + (0b000011 << 6) + (0b000101 << 12);
                    _voxels[x, y, z]._quadType = 16;
                    _voxels[x, y, z]._isTri = true;
                    break;
                case 38:
                    _voxels[x, y, z]._type = VoxelTypes.CornerBackLeftInv;
                    _voxels[x, y, z]._occupiedFaces = 0b000100 + (0b000100 << 9) + (0b000011 << 15);
                    _voxels[x, y, z]._quadType = 20;
                    _voxels[x, y, z]._isTri = true;
                    break;
                case 26:
                    _voxels[x, y, z]._type = VoxelTypes.CornerBackRight;
                    _voxels[x, y, z]._occupiedFaces = (0b000101 << 3) + (0b000101 << 6) + (0b000010 << 15);
                    _voxels[x, y, z]._quadType = 15;
                    _voxels[x, y, z]._isTri = true;
                    break;
                case 27:
                    _voxels[x, y, z]._type = VoxelTypes.SlopeBack;
                    _voxels[x, y, z]._occupiedFaces = (0b000001 << 3) + (0b000001 << 6) + (0b000101 << 12) + (0b000010 << 15);
                    _voxels[x, y, z]._quadType = 8;
                    break;
                case 29:
                    _voxels[x, y, z]._type = VoxelTypes.SlopeHorizontalForward;
                    _voxels[x, y, z]._occupiedFaces = 0b000101 + (0b000010 << 3) + (0b000001 << 6) + (0b000001 << 12);
                    _voxels[x, y, z]._quadType = 22;
                    break;
                case 30:
                    _voxels[x, y, z]._type = VoxelTypes.SlopeHorizontalRight;
                    _voxels[x, y, z]._occupiedFaces = 0b000010 + (0b000101 << 3) + (0b000001 << 6) + (0b000001 << 15);
                    _voxels[x, y, z]._quadType = 23;
                    break;
                case 37:
                    _voxels[x, y, z]._type = VoxelTypes.CornerForwardLeftInv;
                    _voxels[x, y, z]._occupiedFaces = 0b000011 + (0b000010 << 9) + (0b000100 << 12);
                    _voxels[x, y, z]._quadType = 21;
                    _voxels[x, y, z]._isTri = true;
                    break;
                case 40:
                case 43:
                    _voxels[x, y, z]._type = VoxelTypes.SlopeForward;
                    _voxels[x, y, z]._occupiedFaces = (0b000001 << 3) + (0b000001 << 9) + (0b000011 << 12) + (0b000101 << 15);
                    _voxels[x, y, z]._quadType = 6;
                    break;
                case 41:
                    _voxels[x, y, z]._type = VoxelTypes.CornerForwardRight;
                    _voxels[x, y, z]._occupiedFaces = (0b000100 << 3) + (0b000101 << 9) + (0b000011 << 12);
                    _voxels[x, y, z]._quadType = 14;
                    _voxels[x, y, z]._isTri = true;
                    break;
                case 42:
                    _voxels[x, y, z]._type = VoxelTypes.CornerForwardLeft;
                    _voxels[x, y, z]._occupiedFaces = (0b000011 << 3) + (0b000011 << 9) + (0b000101 << 15);
                    _voxels[x, y, z]._quadType = 17;
                    _voxels[x, y, z]._isTri = true;
                    break;
                case 45:
                    _voxels[x, y, z]._type = VoxelTypes.SlopeHorizontalLeft;
                    _voxels[x, y, z]._occupiedFaces = 0b000011 + (0b000100 << 3) + (0b000001 << 9) + (0b000001 << 12);
                    _voxels[x, y, z]._quadType = 25;
                    break;
                case 46:
                    _voxels[x, y, z]._type = VoxelTypes.SlopeHorizontalBack;
                    _voxels[x, y, z]._occupiedFaces = 0b000100 + (0b000011 << 3) + (0b000001 << 9) + (0b000001 << 15);
                    _voxels[x, y, z]._quadType = 24;
                    break;
            }
        }

        bool CheckGridConnection(Vector3Int center)
        {
            Vector3Int[] offsets = new Vector3Int[6]
            {
                new Vector3Int(1,0,0),
                new Vector3Int(-1,0,0),
                new Vector3Int(0,1,0),
                new Vector3Int(0,-1,0),
                new Vector3Int(0,0,1),
                new Vector3Int(0,0,-1)
            };

            List<Vector3Int> adjacentVoxels = new List<Vector3Int>();
            for (int i = 0; i < 6; ++i)
            {
                Vector3Int centerCopy = center + offsets[i];
                if (_voxels[centerCopy.x, centerCopy.y, centerCopy.z]._type != VoxelTypes.Empty)
                    adjacentVoxels.Add(centerCopy);
            }
            bool[,,] gridChecked = new bool[_dimentions.x, _dimentions.y, _dimentions.z];

            // Recursively check adjacent voxels
            void CheckSpace(Vector3Int coord)
            {
                gridChecked[coord.x, coord.y, coord.z] = true;
                adjacentVoxels.Remove(coord);
                if (adjacentVoxels.Count == 0)
                    return;
                // Check adjacent spaces
                for (int i = 0; i < 6; ++i)
                {
                    Vector3Int centerCopy = coord + offsets[i];
                    if (gridChecked[centerCopy.x, centerCopy.y, centerCopy.z] == false && _voxels[centerCopy.x, centerCopy.y, centerCopy.z]._type != VoxelTypes.Empty)
                        CheckSpace(new Vector3Int(centerCopy.x, centerCopy.y, centerCopy.z));
                }
            }
            CheckSpace(adjacentVoxels[0]);

            if (adjacentVoxels.Count == 0)
            {
                return true;
            }

            // Remove disconnected portion from grid
            void RemoveFromGrid(VoxelMesh voxelInstance, Vector3Int coord)
            {
                // Remove from current grid & add to new grid
                voxelInstance._voxels[coord.x, coord.y, coord.z]._type = _voxels[coord.x, coord.y, coord.z]._type;
                ++voxelInstance._blockCount;
                --_blockCount;
                _voxels[coord.x, coord.y, coord.z]._type = VoxelTypes.Empty;
                voxelInstance.UpdateBounds(coord);
                adjacentVoxels.Remove(coord);
                for (int i = 0; i < 6; ++i)
                {
                    Vector3Int centerCopy = coord + offsets[i];
                    if (_voxels[centerCopy.x, centerCopy.y, centerCopy.z]._type != VoxelTypes.Empty)
                        RemoveFromGrid(voxelInstance, new Vector3Int(centerCopy.x, centerCopy.y, centerCopy.z));
                }
            }
            while (adjacentVoxels.Count != 0)
            {
                GameObject newVoxelMesh = GameObject.Instantiate(_prefab, transform.position, transform.rotation);
                VoxelMesh newVoxelhInstance = newVoxelMesh.GetComponent<VoxelMesh>();
                RemoveFromGrid(newVoxelhInstance, adjacentVoxels[0]);
                newVoxelhInstance.CenterGrid();
                newVoxelhInstance.MeshMarch();
                newVoxelhInstance.GenerateMesh();
            }
            MeshMarch();

            return false;
        }

        void UpdateBounds(Vector3Int coords)
        {
            boundsMinX = Math.Min(boundsMinX, coords.x);
            boundsMaxX = Math.Max(boundsMaxX, coords.x);
            boundsMinY = Math.Min(boundsMinY, coords.y);
            boundsMaxY = Math.Max(boundsMaxY, coords.y);
            boundsMinZ = Math.Min(boundsMinZ, coords.z);
            boundsMaxZ = Math.Max(boundsMaxZ, coords.z);
        }

        void CenterGrid()
        {
            Vector3Int offset = new Vector3Int((_dimentions.x - boundsMaxX - boundsMinX) / 2,
                                               (_dimentions.y - boundsMaxY - boundsMinY) / 2,
                                               (_dimentions.z - boundsMaxZ - boundsMinZ) / 2);
            print(offset);
            TransposeGrid(offset);
        }

        void TransposeGrid(Vector3Int offset)
        {
            if (offset == Vector3Int.zero)
            {
                return;
            }

            if (boundsMinX + offset.x < 1 || boundsMaxX + offset.x > _dimentions.x - 2
                || boundsMinY + offset.y < 1 || boundsMaxY + offset.y > _dimentions.y - 2
                || boundsMinZ + offset.z < 1 || boundsMaxZ + offset.z > _dimentions.z - 2)
            {
                Debug.LogError("Transpose is out of bounds \n" + "Offset: " + offset +
                    "\nBounds Min X: " + boundsMinX + "Bounds Max X" + boundsMaxX +
                    "\nBounds Min Y: " + boundsMinY + "Bounds Max Y" + boundsMaxY +
                    "\nBounds Min Z: " + boundsMinZ + "Bounds Max Z" + boundsMaxZ);
                return;
            }

            int xDir = Mathf.Clamp(-offset.x, -1, 1);
            int yDir = Mathf.Clamp(-offset.y, -1, 1);
            int zDir = Mathf.Clamp(-offset.z, -1, 1);
            xDir = xDir == 0 ? -1 : xDir;
            yDir = yDir == 0 ? -1 : yDir;
            zDir = zDir == 0 ? -1 : zDir;
            // Transpose voxels
            for (int x = xDir > 0 ? 1 : _dimentions.x - 1; xDir > 0 ? x < _dimentions.x - 1 : x > 0; x += xDir)
            {
                for (int y = yDir > 0 ? 1 : _dimentions.y - 1; yDir > 0 ? y < _dimentions.y - 1 : y > 0; y += yDir)
                {
                    for (int z = zDir > 0 ? 1 : _dimentions.z - 1; zDir > 0 ? z < _dimentions.z - 1 : z > 0; z += zDir)
                    {
                        if (_voxels[x, y, z]._type != VoxelTypes.Empty)
                        {
                            _voxels[x + offset.x, y + offset.y, z + offset.z] = _voxels[x, y, z];
                            _voxels[x, y, z] = _voxels[0, 0, 0]; // Zero out voxel
                        }
                    }
                }
            }
            // Update bounds
            boundsMinX += offset.x;
            boundsMaxX += offset.x;
            boundsMinY += offset.y;
            boundsMaxY += offset.y;
            boundsMinZ += offset.z;
            boundsMaxZ += offset.z;
            // Return to original position
            transform.position -= offset;
            GenerateMesh();
        }

        void ResizeGrid(Vector3Int newDimensions)
        {
            if (newDimensions == _dimentions)
            {
                return;
            }

            if (newDimensions.x < 0 || newDimensions.y < 0 || newDimensions.z < 0)
                Debug.LogError("New dimensions are negative\n" + "New Dimensions: " + newDimensions);
            if (boundsMaxX - boundsMinX > newDimensions.x - 2 || boundsMaxY - boundsMinY > newDimensions.y - 2 || boundsMaxZ - boundsMinZ > newDimensions.z - 2)
            {
                Debug.LogError("New dimension puts current grid out of bounds \n" + "New Dimensions: " + newDimensions +
                    "\nWidth: " + (boundsMaxX - boundsMinX) + "\nHeight: " + (boundsMaxY - boundsMinY) + "\nDepth: " + (boundsMaxZ - boundsMinZ));
                return;
            }

            Voxel[,,] newVoxels = new Voxel[newDimensions.x, newDimensions.y, newDimensions.z];
            for (int x = 0; x < newDimensions.x; ++x)
            {
                for (int y = 0; y < newDimensions.y; ++y)
                {
                    for (int z = 0; z < newDimensions.z; ++z)
                    {
                        newVoxels[x, y, z] = new Voxel();
                    }
                }
            }
            // Copy over current grid
            Vector3Int offset = new Vector3Int((_dimentions.x - boundsMaxX - boundsMinX) / 2,
                                               (_dimentions.y - boundsMaxY - boundsMinY) / 2,
                                               (_dimentions.z - boundsMaxZ - boundsMinZ) / 2);
            for (int x = boundsMinX; x <= boundsMaxX; ++x)
            {
                for (int y = boundsMinY; y <= boundsMaxY; ++y)
                {
                    for (int z = boundsMinZ; z <= boundsMaxZ; ++z)
                    {
                        newVoxels[x + offset.x, y + offset.y, z + offset.z] = _voxels[x, y, z];
                    }
                }
            }
            _voxels = newVoxels;
            // Update bounds
            boundsMinX += offset.x;
            boundsMaxX += offset.x;
            boundsMinY += offset.y;
            boundsMaxY += offset.y;
            boundsMinZ += offset.z;
            boundsMaxZ += offset.z;
            // Return to original position
            transform.position -= offset;
            // Update grid
            GenerateMesh();
            _dimentions = newDimensions;
        }

        void PaintVoxel(Vector3 coord, Color color)
        {
            Vector3Int roundedCoord = new Vector3Int(Mathf.RoundToInt(coord.x), Mathf.RoundToInt(coord.y), Mathf.RoundToInt(coord.z));

            VoxelTypes selectedVoxel = _voxels[roundedCoord.x, roundedCoord.y, roundedCoord.z]._type;
            print("Paint: " + selectedVoxel);

            if (selectedVoxel != VoxelTypes.Empty)
                _texture.SetPixel(roundedCoord.x, roundedCoord.y/* + _dimentions.y * roundedCoord.z*/, color);
            else
            {
                Vector3Int alternativeVoxel = new Vector3Int(RoundToIntAltUp(coord.x), RoundToIntAltUp(coord.y), RoundToIntAltUp(coord.z));
                print("Paint: " + alternativeVoxel);
                _texture.SetPixel(alternativeVoxel.x, alternativeVoxel.y/* + _dimentions.y * alternativeVoxel.z*/, color);
            }
            _texture.Apply();
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
        // Similar to Mathf.RoundToInt, but rounds Up for 0.5f
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

        private void OnDrawGizmosSelected()
        {
            // Draw Outer Bounds of Grid
            Gizmos.color = new Color(1, 0, 0, 1);
            Gizmos.DrawWireCube(transform.position + new Vector3(_dimentions.x / 2, _dimentions.y / 2, _dimentions.z / 2), new Vector3(_dimentions.x, _dimentions.y, _dimentions.z));
        }
    }
}
