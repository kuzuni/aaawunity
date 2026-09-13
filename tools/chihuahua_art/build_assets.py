#!/usr/bin/env python3
"""T513 아트 후처리: 원본 좌표 보존, PSB 레이어, 정사각 썸네일 및 .meta.

pip install -r tools/chihuahua_art/requirements.txt
python tools/chihuahua_art/build_assets.py --state /path/to/generation-state.json
"""
import argparse
import hashlib
import importlib.util
import json
import struct
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage as ndi
from scipy.optimize import linear_sum_assignment
from psd_tools import PSDImage
from psd_tools.api.layers import PixelLayer
from psd_tools.constants import Compression, Tag

ROOT=Path(__file__).resolve().parents[2]
ASSET_ROOT=ROOT/'Assets/Art/ChihuahuaEquipmentThemes'
NAMES=['무기','머리','몸통','팔1','팔2','다리1','다리2','배경']
ENGLISH=['Weapon','Head','Body','Arm1','Arm2','Leg1','Leg2','Background']
ANCHORS=np.array([(235,260),(720,245),(730,735),(140,1050),(390,1120),(655,1195),(945,1195)],float)/[1155,1362]
PSD_IMPORTER_GUID='b2a9591990af98743ba3ff7cf1000886'

def normalize_weapon(source_path,reference_path,out_path):
    """무기의 대각선 길이/손잡이 끝을 기준 원본에 맞추고 폭은 보존한다."""
    source=Image.open(source_path).convert('RGB')
    reference=Image.open(reference_path).convert('RGB')
    if source.size!=reference.size:
        if max(abs(a-b) for a,b in zip(source.size,reference.size))>2:
            raise ValueError('Reference canvas mismatch')
        # Some generations differ by one edge pixel; pad/crop without rescaling any part.
        canvas=Image.new('RGB',reference.size,'white');canvas.paste(source,(0,0));source=canvas
    parts,_,_=split_parts(source)
    base_parts,_,_=split_parts(reference)
    weapon=parts[0];base=base_parts[0]
    def points(part):
        yy,xx=np.where(np.asarray(part['image'])[:,:,3]>=128)
        return np.column_stack((xx+part['bbox'][0],yy+part['bbox'][1])).astype(float)
    original=points(base)
    _,vectors=np.linalg.eigh(np.cov(original.T))
    axis=vectors[:,-1]
    if axis[1]<0:axis=-axis
    across=np.array([axis[1],-axis[0]])
    # All generated sheets use the reference canvas. Fail rather than stretch a character.
    if source.size!=reference.size:raise ValueError('Reference canvas mismatch')
    target=original@axis;current=points(weapon);projection=current@axis
    target_length=float(np.ptp(target));current_length=float(np.ptp(projection))
    scale=target_length/current_length
    transform=np.eye(2)+(scale-1)*np.outer(axis,axis)
    original_grip=original[target>target.max()-12].mean(0)
    current_grip=current[projection>projection.max()-12].mean(0)
    offset=axis*(target.max()-scale*projection.max())+across*((original_grip-current_grip)@across)
    full=Image.new('RGBA',source.size)
    full.alpha_composite(weapon['image'],weapon['bbox'][:2])
    inv=np.linalg.inv(transform);shift=-inv@offset
    affine=(inv[0,0],inv[0,1],shift[0],inv[1,0],inv[1,1],shift[1])
    adjusted=full.transform(source.size,Image.Transform.AFFINE,affine,Image.Resampling.BICUBIC)
    clean=np.asarray(source).copy()
    x1,y1,x2,y2=weapon['bbox'];mask=np.asarray(weapon['image'])[:,:,3]>0
    clean[y1:y2,x1:x2][mask]=255
    result=Image.fromarray(clean).convert('RGBA');result.alpha_composite(adjusted)
    result.convert('RGB').save(out_path,optimize=True)
    yy,xx=np.where(np.asarray(adjusted)[:,:,3]>=128)
    actual=float(np.ptp(np.column_stack((xx,yy))@axis))
    if abs(actual-target_length)>2:raise ValueError('Weapon length normalization failed')
    return dict(axis=axis.tolist(),reference_length_px=target_length,
        generated_length_px=current_length,final_length_px=actual,
        length_error_px=abs(actual-target_length),width_scale=1,
        generated_sha256=hashlib.sha256(source_path.read_bytes()).hexdigest())

def save_json(path, value):
    path.write_text(json.dumps(value,ensure_ascii=False,indent=2)+'\n')

def thumbnail(image,size=512,fill=.80):
    image=image.convert('RGBA')
    box=image.getchannel('A').getbbox()
    if box is None:
        raise ValueError('Empty thumbnail')
    crop=image.crop(box)
    scale=min(size*fill/crop.width,size*fill/crop.height)
    crop=crop.resize((max(1,round(crop.width*scale)),max(1,round(crop.height*scale))),Image.Resampling.LANCZOS)
    result=Image.new('RGBA',(size,size))
    result.alpha_composite(crop,((size-crop.width)//2,(size-crop.height)//2))
    return result

def split_parts(source):
    rgb=np.asarray(source.convert('RGB'))
    h,w=rgb.shape[:2]
    labels,n=ndi.label(rgb.min(2)<180,structure=np.ones((3,3)))
    areas=np.bincount(labels.ravel());areas[0]=0
    candidates=np.argsort(areas)[-7:]
    if len(candidates)!=7 or min(areas[candidates])<1000:
        raise ValueError('Seven substantial sprite contours not found')
    centers=[]
    for c in candidates:
        yy,xx=np.where(labels==c)
        centers.append([(xx.min()+xx.max())/(2*w),(yy.min()+yy.max())/(2*h)])
    distance=np.linalg.norm(ANCHORS[:,None,:]-np.array(centers)[None,:,:],axis=2)
    rows,cols=linear_sum_assignment(distance)
    if max(distance[rows,cols])>.10:
        raise ValueError('Sprite layout drift exceeds expected rig regions')
    components={int(i):int(candidates[j]) for i,j in zip(rows,cols)}
    parts=[];occupied=np.zeros((h,w),bool);background=rgb.copy()
    for i in range(7):
        solid=ndi.binary_fill_holes(labels==components[i])
        support=ndi.binary_dilation(solid,iterations=2)
        interior=ndi.binary_erosion(solid,iterations=2)
        edge=support&~interior
        rgba=np.zeros((h,w,4),np.uint8)
        rgba[interior,:3]=rgb[interior];rgba[interior,3]=255
        color=rgb[edge].astype(np.int32);minimum=color.min(1);alpha=255-minimum
        fg=np.zeros_like(color);nonzero=alpha>0
        fg[nonzero]=np.rint((color[nonzero]-minimum[nonzero,None])*255/alpha[nonzero,None]).astype(np.int32)
        rgba[edge,:3]=np.clip(fg,0,255).astype(np.uint8);rgba[edge,3]=alpha.astype(np.uint8)
        mask=rgba[:,:,3]>0
        if np.any(mask&occupied):raise ValueError('Sprite masks overlap')
        occupied|=mask;background[mask]=255
        yy,xx=np.where(mask);box=(int(xx.min()),int(yy.min()),int(xx.max()+1),int(yy.max()+1))
        parts.append(dict(name=NAMES[i],key=ENGLISH[i],id=1001+i,bbox=box,image=Image.fromarray(rgba).crop(box)))
    # An unassigned dark region would become a stranded item in the background.
    missing=(rgb.min(2)<140)&~ndi.binary_dilation(occupied,iterations=1)
    extra,extra_n=ndi.label(missing)
    extra_sizes=np.bincount(extra.ravel());extra_sizes[0]=0
    if extra_sizes.max(initial=0)>100:
        raise ValueError(f'Unassigned foreground contour: {extra_sizes.max()} pixels')
    composite=Image.fromarray(background).convert('RGBA')
    for part in reversed(parts):composite.alpha_composite(part['image'],part['bbox'][:2])
    delta=np.abs(np.asarray(composite.convert('RGB')).astype(np.int16)-rgb.astype(np.int16))
    if delta.max()>1:raise ValueError('Source reconstruction differs')
    return parts,Image.fromarray(background).convert('RGBA'),int(delta.max())

def build_psb(source_path,out_path):
    source=Image.open(source_path).convert('RGB')
    parts,background,delta=split_parts(source)
    psb=PSDImage.new('RGBA',source.size,color=(0,0,0,0),compression=Compression.ZIP)
    psb._record.header.version=2
    psb._background_color=None
    layer=PixelLayer.frompil(background,psb,name='Background',compression=Compression.ZIP)
    layer.name='배경';layer._record.name='08_Background'
    layer.tagged_blocks.set_data(Tag.LAYER_ID,1008);layer.visible=False
    for part in reversed(parts):
        x,y,_,_=part['bbox']
        layer=PixelLayer.frompil(part['image'],psb,name=part['key'],left=x,top=y,compression=Compression.ZIP)
        layer.name=part['name'];layer._record.name=f"{part['id']-1000:02d}_{part['key']}"
        layer.tagged_blocks.set_data(Tag.LAYER_ID,part['id'])
    psb.save(out_path)
    with out_path.open('rb') as f:
        if struct.unpack('>4sH',f.read(6))!=(b'8BPS',2):raise ValueError('Not a real PSB')
    loaded=PSDImage.open(out_path)
    if loaded.size!=source.size:raise ValueError('Canvas changed')
    if [x.name for x in reversed(list(loaded))]!=NAMES:raise ValueError('Layer order mismatch')
    lookup={x.name:x for x in loaded}
    if lookup['배경'].visible:raise ValueError('Background must be hidden for Unity')
    for part in parts:
        layer=lookup[part['name']]
        if layer.bbox!=part['bbox'] or layer.layer_id!=part['id']:raise ValueError('Layer geometry/id mismatch')
        np.testing.assert_array_equal(np.asarray(layer.topil()),np.asarray(part['image']))
    return parts,dict(canvas=list(source.size),layers_top_to_bottom=NAMES,
        source_sha256=hashlib.sha256(source_path.read_bytes()).hexdigest(),
        psb_sha256=hashlib.sha256(out_path.read_bytes()).hexdigest(),
        background_visible=False,source_reconstruction_max_channel_difference=delta,
        parts=[{k:v for k,v in p.items() if k!='image'} for p in parts])

def psb_meta():
    return ('ScriptedImporter:\n  internalIDToNameTable: []\n  externalObjects: {}\n'
        '  serializedVersion: 2\n  userData: T513 ChihuahuaEquipmentThemes\n'
        '  assetBundleName: \n  assetBundleVariant: \n'
        f'  script: {{fileID: 11500000, guid: {PSD_IMPORTER_GUID}, type: 3}}\n'
        '  m_MosaicLayers: 1\n  m_CharacterMode: 1\n  m_ImportHiddenLayers: 0\n'
        '  m_LayerMappingOption: 2\n  m_GenerateGOHierarchy: 1\n  m_ResliceFromLayer: 0\n')

def ensure_meta():
    spec=importlib.util.spec_from_file_location('project_gen_meta',ROOT/'tools/gen_meta.py')
    generator=importlib.util.module_from_spec(spec);spec.loader.exec_module(generator)
    paths=[ROOT/'Assets/Art',ASSET_ROOT]+list(ASSET_ROOT.rglob('*'))
    for path in paths:
        if path.suffix=='.meta':continue
        meta=Path(str(path)+'.meta')
        if meta.exists():continue
        relative=path.relative_to(ROOT).as_posix()
        body=psb_meta() if path.suffix=='.psb' else generator.body(relative,path.is_dir())
        meta.write_text(f'fileFormatVersion: 2\nguid: {generator.guid(relative)}\n'+body)

def build_theme(theme,state,force=False):
    dest=ASSET_ROOT/theme['id'];source=dest/'rigging_original.png'
    if not source.exists():return None
    if not state['items'].get(theme['id'],{}).get('rig_source'):return None
    thumb_dir=dest/'Thumbnails';thumb_dir.mkdir(exist_ok=True)
    report_path=dest/'layers.json';psb_path=dest/'rigging_layers.psb'
    previous=json.loads(report_path.read_text()) if report_path.exists() else {}
    if force or not psb_path.exists() or not previous.get('weapon_normalization'):
        generated=state['items'].get(theme['id'],{})
        normalization=normalize_weapon(Path(generated['rig_source']),ASSET_ROOT/'Reference/character_base.png',source)
        parts,report=build_psb(source,psb_path)
        report['weapon_normalization']=normalization
        thumbnail(parts[2]['image']).save(thumb_dir/'armor.png',optimize=True)
        thumbnail(parts[0]['image']).save(thumb_dir/'weapon.png',optimize=True)
        report['title']=theme['title'];report['grade']=theme['grade'];report['class']=theme['job']
        save_json(report_path,report)
    else:report=json.loads(report_path.read_text())
    generated=state['items'].get(theme['id'],{})
    for slot in ['hat','ring','necklace','earring']:
        src=generated.get(slot+'_source')
        target=thumb_dir/(slot+'.png')
        if src and (force or not target.exists()):
            image=Image.open(src)
            if image.mode!='RGBA' or image.getchannel('A').getextrema()[0]!=0:
                raise ValueError(f'{theme["id"]}/{slot}: generated thumbnail lacks transparency')
            thumbnail(image).save(target,optimize=True)
    paths={slot:'Thumbnails/'+slot+'.png' for slot in ['hat','armor','ring','necklace','earring','weapon']}
    complete=all((dest/path).exists() for path in paths.values())
    record={k:theme[k] for k in ['id','title','grade','job']}
    record.update(original='rigging_original.png',psb='rigging_layers.psb',thumbnails=paths,complete=complete)
    save_json(dest/'equipment.json',record)
    save_json(dest/'design.json',theme)
    return record

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--state',type=Path,required=True)
    parser.add_argument('--thumbnail-state',type=Path,action='append',default=[])
    parser.add_argument('--theme')
    parser.add_argument('--force',action='store_true')
    parser.add_argument('--require-complete',action='store_true')
    args=parser.parse_args()
    from make_plan import make_plan
    state=json.loads(args.state.read_text());records=[];errors=[]
    for thumbnail_state in args.thumbnail_state:
        for key,items in json.loads(thumbnail_state.read_text())['items'].items():
            state['items'].setdefault(key,{}).update(items)
    for theme in make_plan():
        if args.theme and args.theme!=theme['id']:continue
        try:
            record=build_theme(theme,state,args.force)
            if record:records.append(record);print(theme['id'],'complete' if record['complete'] else 'rig-ready',flush=True)
        except Exception as error:
            errors.append({'theme':theme['id'],'error':str(error)});print('ERROR',theme['id'],str(error),flush=True)
    ensure_meta()
    if not args.theme:
        save_json(ASSET_ROOT/'catalog.json',{'schema':1,'layer_order':NAMES,'themes':records})
        ensure_meta()
    summary={'rigs_built':len(records),'complete':sum(x['complete'] for x in records),'errors':errors}
    print(json.dumps(summary,ensure_ascii=False))
    if errors or (args.require_complete and (len(records)!=27 or summary['complete']!=27)):
        raise SystemExit(1)

if __name__=='__main__':main()
