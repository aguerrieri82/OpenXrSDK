using Silk.NET.Maths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using XrEngine;
using XrMath;
using static XrSamples.Earth.SceneConst;

namespace XrSamples.Earth
{
    public class Planet : Group3D
    {
        protected TriangleMesh? _sphere;

        public Planet()
        {
            SubLevels = 3;
        }

        protected void Create()
        {
            if (AtmosphereHeight > 0)
                AddAtmosphere();
            _sphere = AddSphere();
        }

        public Object3D CreateOrbit(Color color)
        {
            var mesh = Orbit!.CreateGeometry(color, 0.0001f);
            mesh.Name = "Orbit " + Name;
            return mesh;
        }

        protected TriangleMesh AddSphere()
        {
            var mat = MaterialFactory.CreatePbr(BaseColor);
            mat.ColorMap = Albedo;

            if (mat is IHeightMaterial heightMat)
                heightMat.HeightMap = HeightMap;

            if (RoughnessMap != null)
            {

            }

            Geometry3D sphere = HeightMap != null ? 
                new QuadSphere3D(SphereRadius, SubLevels) : 
                new Sphere3D(SphereRadius, 50);
            var mesh = new TriangleMesh(sphere, (Material)mat);
            mesh.Name = "Planet";

            return AddChild(mesh);    
        }

        protected void AddAtmosphere()
        {
            var mesh = new TriangleMesh();

            mesh.Geometry = new Cube3D();
            mesh.Name = "Atmosphere";   
            mesh.Materials.Add(new GlowVolumeMaterial()
            {
                SphereRadius = SphereRadius,
                HaloWidth = AtmosphereHeight,
                HaloColor = AtmosphereColor,
                UseDepthCulling = true,
                BlendColor = true,
                FadeMode = GlowFadeMode.Exp,
                StepSize = SphereRadius / 1000
            });

            mesh.Transform.SetScale(SphereRadius * 2);   

            AddChild(mesh);

        }

        public void AddTile(string heightPath, string? roughPath, string? colorPath)
        {
            var store = Context.Require<IAssetStore>();

            var tile = new GeoTile();

            tile.SphereRadius = SphereRadius;

            if (roughPath!= null)
            {
                tile.Roughness = AssetLoader.Instance.Load<Texture2D>(store.GetPath(roughPath));
                tile.Roughness.MipLevelCount = 20;
                tile.Roughness.MinFilter = ScaleFilter.LinearMipmapLinear;
            }
        
            if (colorPath != null)
            {
                tile.Color = AssetLoader.Instance.Load<Texture2D>(store.GetPath(colorPath));
                tile.Color.Format = TextureFormat.SBgra32;
                tile.Color.MipLevelCount = 20;
                tile.Color.MinFilter = ScaleFilter.LinearMipmapLinear;
                tile.Color.WrapS = WrapMode.ClampToEdge;
                tile.Color.WrapS = WrapMode.ClampToEdge;
            }

            tile.LoadGeoTiff(store.GetPath(heightPath));

            if (colorPath == null && Albedo != null)
                tile.LoadAlbedoSlice(Albedo);

            AddChild(tile); 
        }

        public virtual float RotationAngle(DateTime utcTime)
        {

            return 0;
        }

        public override void Update(RenderContext ctx)
        {

            _transform.Orientation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, AxisTilt) *
                                     Quaternion.CreateFromAxisAngle(Vector3.UnitY, Rotation + RotationOffset);

            if (_sphere!.Materials[0] is IHeightMaterial hm && hm.HeightMap != null)
                hm.HeightMap!.SphereWorldCenter = WorldPosition;

            foreach (var tile in Children.OfType<GeoTile>())
                tile.SphereWorldCenter = WorldPosition;

            base.Update(ctx);
        }

      


        public float AtmosphereHeight { get; set; }

        public Color AtmosphereColor { get; set; }

        public float SphereRadius { get; set; }



        [ValueType(XrEngine.ValueType.Radiant)]
        public float AxisTilt { get; set; } 

        public Texture2D? Albedo { get; set; }

        public Texture2D? RoughnessMap { get; set; }

        public HeightMapSettings? HeightMap { get; set; }

        public Orbit? Orbit { get; set; }    

        public Color BaseColor { get; set; }

        [ValueType(XrEngine.ValueType.Radiant)]
        public float Rotation { get; set; }


        public int SubLevels { get; set; }


        [ValueType(XrEngine.ValueType.Radiant)]
        public float RotationOffset { get; set; }
    }
}
