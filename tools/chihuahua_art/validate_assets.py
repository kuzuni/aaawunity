#!/usr/bin/env python3
"""저장된 아트 27세트의 완전성 및 원본↔PSB를 독립적으로 검사한다."""
import hashlib
import json
from pathlib import Path

import numpy as np
from PIL import Image
from psd_tools import PSDImage

ROOT=Path(__file__).resolve().parents[2]
ART=ROOT/'Assets/Art/ChihuahuaEquipmentThemes'
ORDER=['무기','머리','몸통','팔1','팔2','다리1','다리2','배경']
SLOTS=['hat','armor','ring','necklace','earring','weapon']

def main():
    catalog=json.loads((ART/'catalog.json').read_text())
    assert len(catalog['themes'])==27,'27 themes required'
    assert len({x['id'] for x in catalog['themes']})==27,'Duplicate theme'
    report=[]
    for record in catalog['themes']:
        dest=ART/record['id']
        assert record['complete'],record['id']
        assert json.loads((dest/'equipment.json').read_text())==record
        info=json.loads((dest/'layers.json').read_text())
        png=dest/record['original'];psb_path=dest/record['psb']
        assert hashlib.sha256(png.read_bytes()).hexdigest()==info['source_sha256']
        assert hashlib.sha256(psb_path.read_bytes()).hexdigest()==info['psb_sha256']
        assert psb_path.read_bytes()[:6]==b'8BPS\x00\x02'
        source=Image.open(png).convert('RGB')
        psb=PSDImage.open(psb_path)
        assert source.size==psb.size==(1155,1362)
        assert [x.name for x in reversed(list(psb))]==ORDER
        assert [x.layer_id for x in reversed(list(psb))]==list(range(1001,1009))
        assert psb[0].name=='배경' and not psb[0].visible
        composite=psb[0].topil().convert('RGBA')
        for layer in list(psb)[1:]:
            assert layer.visible and layer.width>0 and layer.height>0
            composite.alpha_composite(layer.topil().convert('RGBA'),layer.bbox[:2])
        difference=np.abs(np.asarray(composite.convert('RGB')).astype(int)-np.asarray(source).astype(int))
        assert difference.max()<=1,(record['id'],int(difference.max()))
        for slot in SLOTS:
            path=dest/record['thumbnails'][slot]
            image=Image.open(path)
            assert image.size==(512,512) and image.mode=='RGBA',(record['id'],slot)
            alpha=np.asarray(image)[:,:,3]
            assert alpha.min()==0 and alpha.max()==255
            assert max(alpha[0].max(),alpha[-1].max(),alpha[:,0].max(),alpha[:,-1].max())==0
            assert np.count_nonzero(alpha)>512,'Empty or tiny icon'
        assert info['weapon_normalization']['length_error_px']<=2
        report.append({'id':record['id'],'psb_layers':len(psb),'thumbnails':6,
            'composite_max_difference':int(difference.max()),
            'weapon_length_error_px':info['weapon_normalization']['length_error_px']})
        print(record['id'],'OK',flush=True)
    meta_guids=[]
    for path in ART.rglob('*'):
        if path.suffix=='.meta':
            guid=next(x[6:] for x in path.read_text().splitlines() if x.startswith('guid: '))
            meta_guids.append(guid)
        else:assert Path(str(path)+'.meta').is_file(),f'Missing meta: {path}'
    assert len(meta_guids)==len(set(meta_guids)),'Duplicate Unity GUID'
    result={'themes':27,'rigging_pngs':27,'psbs':27,'thumbnails':162,'status':'passed','checks':report}
    print(json.dumps(result,ensure_ascii=False))

if __name__=='__main__':main()
