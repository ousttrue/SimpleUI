﻿using SharpDX;
using SharpDX.Direct3D11;
using System;


namespace Graphics
{
    /// <summary>
    /// テクスチャサイズを変更する前に破棄されるべき
    /// </summary>
    public class D3D11RenderTarget : IDisposable
    {
        Texture2D _texture;
        public Texture2D Texture
        {
            get { return _texture; }
        }
        public SharpDX.DXGI.Surface Surface
        {
            get { return _texture.QueryInterface<SharpDX.DXGI.Surface>(); }
        }
        RenderTargetView _rtv;
        DepthStencilView _dsv;

        public void Dispose()
        {
            if (_rtv != null)
            {
                _rtv.Dispose();
                _rtv = null;
            }

            if (_texture != null)
            {
                _texture.Dispose();
                _texture = null;
            }

            if (_dsv != null)
            {
                _dsv.Dispose();
                _dsv = null;
            }
        }

        Func<Texture2D> _getTexture;
        public D3D11RenderTarget(Func<Texture2D> GetTexture)
        {
            _getTexture = GetTexture;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="texture">take ownership</param>
        /// <param name="count">m_swapChain.Description.SampleDescription</param>
        /// <param name="quality">m_swapChain.Description.SampleDescription</param>
        void Create()
        {
            Dispose();

            // _rtv
            _texture = _getTexture();
            var device = _texture.Device;
            _rtv = new RenderTargetView(_texture.Device, _texture);

            // _dsv
            var desc = _texture.Description;
            using (var depthBuffer = new Texture2D(device, new Texture2DDescription
            {
                Format = SharpDX.DXGI.Format.D24_UNorm_S8_UInt,
                ArraySize = 1,
                MipLevels = 1,
                Width = desc.Width,
                Height = desc.Height,
                SampleDescription = desc.SampleDescription,
                BindFlags = BindFlags.DepthStencil
            }))
            {
                var depthDesc = new DepthStencilViewDescription
                {
                };
                if (desc.SampleDescription.Count > 1 ||
                    desc.SampleDescription.Quality > 0)
                {
                    depthDesc.Dimension = DepthStencilViewDimension.Texture2DMultisampled;
                }
                else
                {
                    depthDesc.Dimension = DepthStencilViewDimension.Texture2D;
                }
                _dsv = new DepthStencilView(device, depthBuffer, depthDesc);
            }
        }

        public static D3D11RenderTarget Create(D3D11Device device, int w, int h)
        {
            return new D3D11RenderTarget(() => new Texture2D(device.Device, new Texture2DDescription
            {
                Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                ArraySize = 1,
                MipLevels = 1,
                Width = w,
                Height = h,
                SampleDescription = new SharpDX.DXGI.SampleDescription
                {
                    Count = 1,
                    Quality = 0
                },
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource
            }));
        }

        public void Setup(D3D11Device device, Color4 clear)
        {
            if (_rtv == null)
            {
                Create();
            }
            device.Context.ClearRenderTargetView(_rtv, clear);
            device.Context.ClearDepthStencilView(_dsv,
                DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil,
                1.0f, 0);

            device.Context.OutputMerger.SetTargets(_dsv, _rtv);
        }
    }
}
