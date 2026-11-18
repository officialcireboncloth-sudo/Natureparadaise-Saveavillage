"""Transfer the original oil-palm pinnae onto curved, alpha-cutout frond cards."""
import bpy
import numpy as np
from mathutils import Vector

from fruit_tree_atlas import (
    OIL_PALM_ATLAS, OIL_PALM_BAKED_ATLAS, OIL_PALM_BAKED_SIZE, OIL_PALM_CHART_PADDING,
    OIL_PALM_FROND_CHARTS, OIL_PALM_SIZE, atlas_uv,
)


MAPS = ('BaseColor', 'Normal', 'SurfaceMask')
SAMPLES = 2


def unit(vectors):
    return vectors / np.maximum(np.linalg.norm(vectors, axis=-1, keepdims=True), 1e-9)


def read_png(path):
    image = bpy.data.images.load(str(path), check_existing=False)
    image.colorspace_settings.name = 'Non-Color'
    image.alpha_mode = 'CHANNEL_PACKED'
    width, height = image.size
    pixels = np.empty(width*height*4, dtype=np.float32)
    image.pixels.foreach_get(pixels)
    bpy.data.images.remove(image)
    return pixels.reshape(height, width, 4)[::-1].copy()


def write_png(path, pixels):
    height, width, _ = pixels.shape
    image = bpy.data.images.new(path.stem, width, height, alpha=True)
    image.colorspace_settings.name = 'Non-Color'
    image.alpha_mode = 'CHANNEL_PACKED'
    image.pixels.foreach_set(np.ascontiguousarray(pixels[::-1], dtype=np.float32).ravel() / 255)
    image.filepath_raw = str(path)
    image.file_format = 'PNG'
    image.save()
    bpy.data.images.remove(image)


def project_strip(vertices, center):
    """Unwrap the curved midrib using longitudinal distance and signed side offset."""
    def sample(t):
        if t < 0:
            return center(0) + (center(.01)-center(0))*(t/.01)
        if t > 1:
            return center(1) + (center(1)-center(.99))*((t-1)/.01)
        return center(t)

    axis = np.array([sample(t) for t in np.linspace(.11, 1.22, 72)])
    steps = np.diff(axis, axis=0)
    distances = np.concatenate(([0], np.cumsum(np.linalg.norm(steps, axis=1))))
    offsets = vertices[:,None,:] - axis[None,:-1,:]
    fraction = np.clip((offsets*steps).sum(-1) / (steps*steps).sum(-1), 0, 1)
    delta = offsets - fraction[:,:,None]*steps
    segment = (delta*delta).sum(-1).argmin(axis=1)
    index = np.arange(len(vertices))
    tangent = unit(steps[segment])
    lateral = unit(np.cross((0,0,1), tangent))
    normal = np.cross(tangent, lateral)
    nearest = delta[index, segment]
    across = (nearest*lateral).sum(-1)
    depth = (nearest*normal).sum(-1)
    along = distances[segment] + fraction[index,segment]*np.diff(distances)[segment]
    return along, across, depth, axis, distances, tangent, lateral, normal


def interpolate_axis(axis, distances, along):
    return np.array([np.interp(along, distances, axis[:,component]) for component in range(3)])


class FrondBaker:
    def __init__(self, source_dir):
        self.source = [read_png(source_dir / f'{OIL_PALM_ATLAS}_{name}.png') for name in MAPS]
        assert all(pixels.shape == (OIL_PALM_SIZE, OIL_PALM_SIZE, 4) for pixels in self.source)
        self.atlas = [np.zeros((OIL_PALM_BAKED_SIZE, OIL_PALM_BAKED_SIZE, 4), dtype=np.uint8)
                      for _ in MAPS]
        for image, pixels in zip(self.atlas, self.source):
            image[:OIL_PALM_SIZE,:OIL_PALM_SIZE] = np.rint(pixels*255).astype(np.uint8)
        self.charts = {}

    def bake(self, slot, source_mesh, center):
        vertices = np.array(source_mesh.vertices)
        distances, across, depth, axis, arc, tangent, lateral, normal = project_strip(vertices, center)
        x, y, width, height = OIL_PALM_FROND_CHARTS[slot]
        border = OIL_PALM_CHART_PADDING
        inner_w, inner_h = width-2*border, height-2*border
        s0, s1 = max(arc[0], distances.min()-.002), min(arc[-1], distances.max()+.004)
        half_width = max(abs(across.min()), abs(across.max()))*1.025
        uv = np.column_stack((.5 + across/(2*half_width), (distances-s0)/(s1-s0)))
        triangles = [(np.array([face[i] for i in corner]),
                      np.array([atlas_uv(name,*coords[i]) for i in corner]))
                     for face, (name,coords) in zip(source_mesh.faces,source_mesh.surfaces)
                     for j in range(1,len(face)-1) for corner in ((0,j,j+1),)]
        samples = np.linspace(s0,s1,9)
        triangle_indices = np.array([indices for indices, _ in triangles])
        s_min = distances[triangle_indices].min(axis=1)
        s_max = distances[triangle_indices].max(axis=1)
        rows, slopes = [], []
        for i, s in enumerate(samples):
            in_strip = (s_min <= samples[min(i+1,8)]) & (s_max >= samples[max(i-1,0)])
            nearby = np.unique(triangle_indices[in_strip])
            origin = interpolate_axis(axis,arc,s)
            t = unit(interpolate_axis(axis,arc,min(arc[-1],s+.012)) -
                     interpolate_axis(axis,arc,max(arc[0],s-.012)))
            side = unit(np.cross((0,0,1),t))
            n = np.cross(t,side)
            points, coords, slopes_at_row = [], [], []
            for sign in (-1,0,1):
                selected = nearby[across[nearby]*sign > .001]
                extent = (sign * min(half_width, max(.012, abs(across[selected]).max()+.008))
                          if len(selected) else sign*.012)
                edge = selected[abs(across[selected]) > abs(extent)*.48]
                bend = np.clip(np.median(depth[edge]), -.23, .10)*.80 if len(edge) else 0
                # The original layers still have screen coverage when seen edge-on.
                # A folded card gives the same wing some downward-facing surface.
                bend = min(bend, -abs(extent)*(.61 + .14*n[2]))
                points.append(origin+side*extent+n*bend)
                coords.append((.5+extent/(2*half_width), (s-s0)/(s1-s0)))
                slopes_at_row.append(bend/extent if extent else 0)
            rows.append((points,coords))
            slopes.append(slopes_at_row)
        self.charts[slot] = rows
        slopes = np.array(slopes)
        tilt = np.where(across<0,np.interp(distances,samples,slopes[:,0]),
                        np.interp(distances,samples,slopes[:,2]))
        folded_lateral = unit(lateral + normal*tilt[:,None])
        folded_normal = np.cross(tangent,folded_lateral)
        pixels = self.rasterize(vertices, triangles, uv, depth, tangent, folded_lateral,
                                folded_normal, inner_w, inner_h)
        for sheet, tile in zip(self.atlas, pixels):
            sheet[y:y+height,x:x+width] = tile

    def rasterize(self, vertices, triangles, uv, depth, tangent, lateral, normal, width, height):
        factor = SAMPLES
        dimensions = np.array((width,height))*factor
        w, h = dimensions
        shape = (h,w,4)
        raw = [np.zeros(shape,np.float32) for _ in MAPS]
        zbuffer = np.full((h,w),-np.inf)
        smooth = np.zeros_like(vertices)
        for ids, _ in triangles:
            a,b,c = vertices[ids]
            np.add.at(smooth,ids,np.cross(b-a,c-a))
        smooth = unit(smooth)
        for ids, source_uv in triangles:
            p = uv[ids]*dimensions
            a,b,c = p
            matrix = np.column_stack((b-a,c-a))
            if abs(np.linalg.det(matrix)) < 1e-7:
                continue
            lo = np.maximum(0,np.floor(p.min(axis=0)).astype(int))
            hi = np.minimum(dimensions-1,np.ceil(p.max(axis=0)).astype(int))
            xx,yy = np.meshgrid(np.arange(lo[0],hi[0]+1),np.arange(lo[1],hi[1]+1))
            bary = (np.stack((xx+.5,yy+.5),axis=-1)-a) @ np.linalg.inv(matrix).T
            inside = ((bary >= -1e-5).all(axis=-1) & (bary.sum(axis=-1) <= 1.00001))
            if not inside.any():
                continue
            coords = bary[inside]
            weights = np.column_stack((1-coords.sum(axis=1),coords))
            projected_depth = weights@depth[ids]
            xx,yy = xx[inside],yy[inside]
            front = projected_depth > zbuffer[yy,xx]
            if not front.any():
                continue
            xx,yy,weights = xx[front],yy[front],weights[front]
            zbuffer[yy,xx] = projected_depth[front]
            old_uv = weights@source_uv
            source_x = np.clip((old_uv[:,0]*OIL_PALM_SIZE).astype(int),0,OIL_PALM_SIZE-1)
            source_y = np.clip(((1-old_uv[:,1])*OIL_PALM_SIZE).astype(int),0,OIL_PALM_SIZE-1)
            base, relief, mask = (image[source_y,source_x].copy() for image in self.source)
            base[:,3] = 1
            a,b,c = vertices[ids]
            d_uv = source_uv[1:]-source_uv[0]
            source_n = unit(weights@smooth[ids])
            dest_n = unit(weights@normal[ids])
            reverse = np.where((source_n*dest_n).sum(-1,keepdims=True) < 0,-1,1)
            source_n *= reverse
            if abs(np.linalg.det(d_uv)) > 1e-10:
                source_axes = np.linalg.solve(d_uv,np.stack((b-a,c-a)))
                du = unit(source_axes[0] - source_n*(source_n*source_axes[0]).sum(-1,keepdims=True))
                dv = unit(source_axes[1] - source_n*(source_n*source_axes[1]).sum(-1,keepdims=True))
                vein = relief[:,:3]*2-1
                source_n = unit(source_n + (du*vein[:,0:1]+dv*vein[:,1:2])*.37)
            dest_u, dest_v = unit(weights@lateral[ids]), -unit(weights@tangent[ids])
            transfer = np.column_stack(((source_n*dest_u).sum(-1), (source_n*dest_v).sum(-1),
                                        np.maximum(.14,(source_n*dest_n).sum(-1))))
            relief[:,:3] = unit(transfer)*.5+.5
            relief[:,3] = 1
            mask[:,1] = 1
            for output,data in zip(raw,(base,relief,mask)):
                output[yy,xx] = data
        coverage = np.isfinite(zbuffer).reshape(height,factor,width,factor).sum(axis=(1,3))
        assert .05 < (coverage > 0).mean() < .82, 'Leaflets should leave clear gaps in the chart'
        downsampled = [image.reshape(height,factor,width,factor,4).sum(axis=(1,3)) /
                       np.maximum(coverage[:,:,None],1) for image in raw]
        downsampled[0][:,:,3] = coverage/(factor*factor)
        return padded_tiles(downsampled,coverage>0)

    def add_card(self, mesh, slot, detail):
        rows = self.charts[slot] if detail else self.charts[slot][::2]
        vertices = [Vector(point) for points,_ in rows for point in points]
        coords = [uv for _,row in rows for uv in row]
        faces = []
        for row in range(len(rows)-1):
            for rail in range(2):
                i = row*3+rail
                quad = (i,i+3,i+4,i+1)
                faces.append((quad,[coords[j] for j in quad]))
        mesh.add(vertices,faces,slot)

    def save(self, directory):
        assert set(self.charts) == set(OIL_PALM_FROND_CHARTS)
        directory.mkdir(parents=True,exist_ok=True)
        for name,pixels in zip(MAPS,self.atlas):
            write_png(directory / f'{OIL_PALM_BAKED_ATLAS}_{name}.png',pixels)


def padded_tiles(images, occupied):
    """Extend RGB under the transparent margin for bilinear filtering and mipmaps."""
    border = OIL_PALM_CHART_PADDING
    occupied = np.pad(occupied,border)
    padded = [np.pad(image,((border,border),(border,border),(0,0))) for image in images]
    alpha = padded[0][:,:,3].copy()
    for _ in range(border):
        grew = occupied.copy()
        for axis, shift in ((0,1),(0,-1),(1,1),(1,-1)):
            fill = np.roll(occupied,shift,axis=axis) & ~grew
            if axis == 0:
                fill[0 if shift > 0 else -1,:] = False
            else:
                fill[:,0 if shift > 0 else -1] = False
            for image in padded:
                image[fill] = np.roll(image,shift,axis=axis)[fill]
            grew |= fill
        occupied = grew
    padded[0][:,:,3] = alpha
    return [np.rint(np.clip(image,0,1)*255).astype(np.uint8) for image in padded]
