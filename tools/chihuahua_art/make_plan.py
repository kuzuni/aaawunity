"""주인 요청 T513: 9등급 × 3직업 치와와 아트 생성 명세."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
GRADES = [
    ('01_Primitive', '원시', 'prehistoric stone, rawhide, bone and vine; handmade and rough, no metal or machines', [
        ('moss green and brown', 'ragged moss-green leather head wrap with one ivory bone pin and two tied tails', 'rough hide vest with green diagonal sash, bone clasp and small belt pouch', 'curved chipped flint knife with vine-bound wooden handle', 'small ivory bone and green stone leaf'),
        ('ochre brown and ivory', 'open-face primitive bone helmet with a short boar-bone crest between the ears', 'thick ochre hide cuirass with overlapping ivory bone plates and rawhide belt', 'single broad stone axe tied to a wooden handle with rawhide', 'ivory bone shield and amber pebble'),
        ('charcoal and muted plum', 'dark rough-hide pointed hood with face and ears fully exposed, small fang fastener', 'charcoal leather wrap vest with plum sash and bone fang clasp', 'long obsidian dagger with a bone handle bound in dark leather', 'obsidian fang and pale bone crescent'),
    ]),
    ('02_Medieval', '중세', 'medieval fantasy leather, iron, steel and simple gemstones; no modern technology', [
        ('forest green and brass', 'soft forest-green open-face thief hood with brass leaf pin', 'green leather jerkin with brown cross strap, brass buckle and coin pouch', 'curved steel cutlass with brass guard and green leather grip', 'brass key and green emerald'),
        ('steel silver and crimson', 'open-face silver knight helmet with compact crimson crest and gold brow trim', 'silver plate cuirass with crimson tabard and gold shield buckle', 'broad straight knight sword with gold crossguard and crimson grip', 'silver shield and red garnet'),
        ('midnight blue and violet', 'pointed midnight-blue assassin hood with violet lining, face fully exposed', 'fitted midnight leather armor with violet crossed straps and silver clasp', 'curved silver stiletto with violet-wrapped grip', 'silver crescent and violet amethyst'),
    ]),
    ('03_EarlyModern', '근대', '17th-19th century early-modern adventure: tailored cloth, polished brass, leather and engraved steel, no electronics', [
        ('burgundy and antique brass', 'burgundy tricorn hat with brass trim and one small ivory feather', 'burgundy waistcoat with cream shirtfront, brown belt and brass coin clasp', 'engraved curved privateer saber with brass knuckle guard', 'antique brass coin and burgundy gem'),
        ('navy and red with gold', 'compact navy military shako with gold badge and short red plume', 'navy officer tunic with red breast panel, gold frogging and brown belt', 'broad officer cavalry saber with gold guard and navy grip', 'gold military crest and red ruby'),
        ('black and muted silver', 'low black wide-brim hat with a silver clasp, fitted between visible ears', 'black tailored duelist vest with silver buttons and dark plum cravat', 'slender engraved steel dueling dagger with black grip', 'silver raven feather and smoky quartz'),
    ]),
    ('04_Modern', '현대', 'present-day urban and tactical equipment, practical synthetic cloth, matte steel and rubber; no sci-fi glow', [
        ('olive green and warm gray', 'olive knit beanie with compact goggles perched on top, eyes unobstructed', 'olive urban utility jacket with gray straps, zipper and compact pouch', 'matte steel curved utility machete with olive rubber grip', 'brushed steel key and green enamel'),
        ('olive drab and gunmetal', 'open-face olive ballistic helmet with a small red insignia patch', 'olive tactical plate carrier with gunmetal chest plate and red shield patch', 'heavy tactical breaching axe with matte black handle and red grip accent', 'gunmetal shield and red enamel'),
        ('matte black and dark red', 'black close-fitting tactical cap with dark red band, face visible', 'black stealth utility vest with dark red zipper line and small silver clasp', 'sleek black tactical combat dagger with silver cutting edge and red grip', 'black steel fang and dark red stone'),
    ]),
    ('05_Cyber', '사이버', 'cyberpunk: dark metal, visible circuit panels and small crisp neon strips, no bloom clouds or floating effects', [
        ('black and electric cyan', 'black hacker headband with angular cyan panel above the eyes', 'black lightweight hacker vest with cyan circuit strips and compact lock module', 'curved cyber blade with cyan edge and black circuit grip', 'black circuit key and cyan chip'),
        ('gunmetal and electric red', 'open-face angular gunmetal cyber helmet with red visor panel above the brow', 'heavy gunmetal cyber breastplate with bold red channels and a central power cell', 'broad cyber energy axe with crisp red blade edge and gunmetal handle', 'gunmetal shield and red power core'),
        ('black and neon magenta', 'black angular stealth hood with short magenta circuit lines, face unobstructed', 'slim black cyber stealth armor with magenta crossed channels and silver clasp', 'narrow black cyber dagger with magenta cutting edge', 'black crescent and magenta circuit gem'),
    ]),
    ('06_Future', '미래', 'advanced clean future technology: smooth white ceramic shells, silver alloy and integrated luminous inlays, more refined than cyberpunk', [
        ('pearl white and turquoise', 'streamlined pearl-white pilot cap with turquoise inset and open face', 'light white ceramic utility suit torso with turquoise inlays and a silver buckle', 'smooth white curved plasma cutlass with turquoise core and silver grip', 'silver key-shaped setting and turquoise crystal'),
        ('pearl white and cobalt blue', 'open-face white exosuit helmet with cobalt crest and gold trim', 'white ceramic exo breastplate with cobalt panels and small gold power crest', 'broad white-and-blue plasma sword with gold guard', 'white ceramic shield and cobalt core'),
        ('silver white and violet', 'sleek white stealth cowl with violet inset, ears and face visible', 'smooth silver-white stealth suit torso with violet precision seams and black belt', 'slim white phase dagger with violet translucent edge', 'silver crescent and violet phase crystal'),
    ]),
    ('07_Space', '우주', 'interstellar adventure: cosmic navy armor, meteor metal, restrained star inlays and colored crystal cores; no scene or floating particles', [
        ('deep navy and teal', 'navy space-pilot cap with teal star badge and silver trim', 'navy space-scavenger suit torso with teal panels and silver star clasp', 'curved meteor-metal saber with teal crystal edge and dark grip', 'silver orbital key and teal meteor crystal'),
        ('cosmic blue and gold', 'open-face cosmic-blue space commander helmet with gold star crest', 'cosmic-blue star-metal breastplate with gold ribs and sapphire core', 'broad star-metal sword with blue stellar core and gold guard', 'gold star shield and sapphire planet gem'),
        ('deep violet and black silver', 'deep-violet void hood with silver star clasp, open face', 'black-violet void suit torso with silver diagonal trim and small purple core', 'dark meteor dagger with violet edge and silver crescent guard', 'black-silver crescent and violet meteor shard'),
    ]),
    ('08_Immortal', '불멸', 'mythic immortal equipment: sacred gold, ivory, jade and ancient eternal emblems; ornate but clean, no background halos or particle effects', [
        ('emerald jade and antique gold', 'emerald immortal head wrap with a gold leaf diadem between ears', 'emerald silk-and-jade light cuirass with antique-gold vine trim and leaf clasp', 'curved immortal jade saber with gold leaf guard', 'antique-gold eternal leaf and emerald jade'),
        ('ivory and sacred gold with crimson', 'open-face ivory immortal helmet with compact gold laurel crown', 'ivory divine breastplate with sacred gold sun crest and crimson sash', 'broad sacred gold-and-ivory sword with red jewel in the guard', 'sacred gold sun shield and crimson ruby'),
        ('obsidian black and antique gold', 'black immortal assassin cowl with antique-gold crescent diadem, open face', 'obsidian ceremonial stealth armor with antique-gold trim and plum sash', 'obsidian immortal dagger with antique-gold crescent guard and purple core', 'antique-gold crescent and obsidian gem'),
    ]),
    ('09_Infinite', '무한', 'ultimate infinity equipment: dimensional black crystal, luminous prismatic inlays, elegant gold geometry and infinity emblems; solid readable silhouettes, no floating extra objects or background aura', [
        ('black crystal and prismatic teal with gold', 'compact black-crystal thief crown with prismatic teal facets and gold infinity clasp', 'light dimensional black armor with teal prism panels and gold infinity belt clasp', 'curved dimensional saber with teal prismatic blade and geometric gold guard', 'gold infinity loop and prismatic teal crystal'),
        ('black gold and prismatic rainbow', 'open-face geometric black-and-gold sovereign helmet with a compact prismatic crown', 'massive-looking black-and-gold infinity cuirass fitted to the rounded torso, large prism crest', 'broad geometric infinity greatsword with a prismatic center and black-gold edge', 'gold infinity shield and multicolor prism'),
        ('obsidian and prismatic violet with silver', 'sharp obsidian assassin cowl with silver infinity diadem and violet facets, face visible', 'sleek dimensional obsidian armor with violet prism seams and silver infinity clasp', 'narrow obsidian infinity dagger with violet prism core and silver geometric guard', 'silver infinity crescent and violet prism'),
    ]),
]
CLASSES = [('Thief','도적'),('Warrior','전사'),('Assassin','암살자')]

def make_plan():
    plan=[]
    for grade_id,grade_ko,era,variants in GRADES:
        for (class_id,class_ko),(palette,hat,armor,weapon,jewel) in zip(CLASSES,variants):
            plan.append(dict(id=f'{grade_id}/{class_id}',grade_id=grade_id,grade=grade_ko,
                class_id=class_id,job=class_ko,title=f'{grade_ko} {class_ko}',era=era,
                palette=palette,hat=hat,armor=armor,weapon=weapon,jewel=jewel))
    return plan

if __name__=='__main__':
    import argparse
    parser=argparse.ArgumentParser()
    parser.add_argument('--output',type=Path,required=True)
    args=parser.parse_args()
    args.output.parent.mkdir(parents=True,exist_ok=True)
    args.output.write_text(json.dumps(make_plan(),ensure_ascii=False,indent=2)+'\n')
    print(f'27 themes: {args.output}')
