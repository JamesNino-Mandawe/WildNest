using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Windows.Forms;
using Project.Homepage_Buttons;

namespace Project
{
    public partial class UcAnimals : UserControl
    {
        // ── ANIMAL MODEL ────────────────────────────────────────────
        private class AnimalData
        {
            public string Name, Species, Zone, Category, Conservation, Desc, Fact, StatA, StatB, StatC, PhotoFile, Emoji;
            public string[] Tags;
            public Color ConsColor;
        }

        // ── STATE ────────────────────────────────────────────────────
        private List<AnimalData> _animals;
        private int _currentIndex = 0;
        private UcAllResidents _ucAllResidents;
        private bool _spotlightActive = true;
        private Image _currentPhoto;

        // ── COLORS ──────────────────────────────────────────────────
        private static readonly Color C_BG = Color.FromArgb(7, 26, 14);
        private static readonly Color C_BG2 = Color.FromArgb(4, 14, 7);
        private static readonly Color C_GOLD = Color.FromArgb(212, 160, 23);
        private static readonly Color C_GOLD2 = Color.FromArgb(232, 181, 50);
        private static readonly Color C_CREAM = Color.FromArgb(248, 244, 239);
        private static readonly Color C_CREAM_DIM = Color.FromArgb(140, 248, 244, 239);
        private static readonly Color C_GREEN = Color.FromArgb(29, 158, 117);
        private static readonly Color C_QUEUE_BG = Color.FromArgb(4, 14, 7);

        public UcAnimals()
        {
            InitializeComponent();
            this.DoubleBuffered = true;
            BuildAnimalData();
            StyleAll();

            // Three-layer guarantee for spotlight button visibility:
            // 1. HandleCreated + BeginInvoke — after handle exists, deferred past layout
            this.HandleCreated += (s, e) =>
            {
                BeginInvoke(new Action(() =>
                {
                    PositionSpotlightBtn();
                    LoadSpotlight(0);
                    // 2. Second deferred call — catches any late Dock recalculation
                    BeginInvoke(new Action(PositionSpotlightBtn));
                }));
            };

            // 3. VisibleChanged on THIS control — fires when UC is shown in parent form
            this.VisibleChanged += (s, e) =>
            {
                if (this.Visible)
                    BeginInvoke(new Action(PositionSpotlightBtn));
            };
        }

        // ════════════════════════════════════════════════════════════
        //  ANIMAL DATA
        // ════════════════════════════════════════════════════════════
        private void BuildAnimalData()
        {
            _animals = new List<AnimalData>
            {
                new AnimalData { Name="African Lion",       Species="Panthera leo",               Emoji="🦁", Zone="Golden Savanna",   Category="Mammal",    Conservation="Least Concern",        ConsColor=Color.FromArgb(29,158,117),  PhotoFile="AfricanLion.jpg",         Desc="WildNest's most iconic predator — a pride of three lions led by 'Malakas'. Two cubs born last month are now visible during morning rounds.", Fact="A lion's roar can be heard from 8 kilometres away — it serves as both territorial warning and pride communication across the savanna.", Tags=new[]{"👑 Pride of 3","🌅 Morning active","🦁 New cubs 2026"}, StatA="8km roar range", StatB="Pride of 3",         StatC="Golden Savanna" },
                new AnimalData { Name="Reticulated Giraffe",Species="Giraffa camelopardalis",     Emoji="🦒", Zone="Golden Savanna",   Category="Mammal",    Conservation="Vulnerable",           ConsColor=Color.FromArgb(230,130,30),  PhotoFile="ReticulatedGiraffe.jpg",  Desc="At 5.5 metres, 'Alon' is WildNest's tallest resident. Guests can hand-feed him from the elevated platform during the 9am and 3pm feeding windows.", Fact="Giraffes have the same number of neck vertebrae as humans — just 7 — but each bone can be over 25cm long.", Tags=new[]{"📏 5.5m tall","🍃 Hand-feeding","⚠ Vulnerable"}, StatA="5.5m tall", StatB="Hand-feeding", StatC="Golden Savanna" },
                new AnimalData { Name="Plains Zebra",       Species="Equus quagga",               Emoji="🦓", Zone="Golden Savanna",   Category="Mammal",    Conservation="Near Threatened",      ConsColor=Color.FromArgb(212,160,23),  PhotoFile="PlainsZebra.jpg",         Desc="A herd of seven zebras grazes the open southern savanna year-round — often in mixed formation with the wildebeest for mutual predator detection.", Fact="No two zebras have identical stripe patterns — each is as unique as a human fingerprint, used by the herd for individual recognition.", Tags=new[]{"🦓 Herd of 7","👀 Predator alert","🌿 Mixed grazers"}, StatA="Herd of 7", StatB="NT Status", StatC="Golden Savanna" },
                new AnimalData { Name="African Elephant",   Species="Loxodonta africana",         Emoji="🐘", Zone="Golden Savanna",   Category="Mammal",    Conservation="Vulnerable",           ConsColor=Color.FromArgb(230,130,30),  PhotoFile="AfricanElephant.jpg",     Desc="'Bundok' is WildNest's only elephant — rescued from a collapsed circus operation in 2017. He now roams a 12-hectare private habitat within the savanna.", Fact="Elephants are one of the few animals that mourn their dead — they return to the bones of deceased companions and caress them with their trunks.", Tags=new[]{"🏥 Rescued 2017","🌿 12ha habitat","🧠 Mourns its dead"}, StatA="12ha range", StatB="Rescued 2017", StatC="Golden Savanna" },
                new AnimalData { Name="Bengal Tiger",       Species="Panthera tigris tigris",     Emoji="🐅", Zone="Predator Ridge",   Category="Mammal",    Conservation="Endangered",           ConsColor=Color.FromArgb(210,70,30),   PhotoFile="BengalTiger.jpg",         Desc="WildNest's most powerful predator — 'Raja' arrived in 2021 from an international sanctuary exchange program. Viewable only from the reinforced observation platform.", Fact="Tigers are the only big cats with striped skin — if you shaved a tiger, its stripe pattern would still be visible on the skin beneath.", Tags=new[]{"🔒 Platform viewing","📸 Best at sunrise","👑 Apex predator"}, StatA="Obs. platform", StatB="Arrived 2021", StatC="Predator Ridge" },
                new AnimalData { Name="Leopard",            Species="Panthera pardus",            Emoji="🐆", Zone="Predator Ridge",   Category="Mammal",    Conservation="Vulnerable",           ConsColor=Color.FromArgb(230,130,30),  PhotoFile="Leopard.jpg",             Desc="The most elusive of WildNest's big cats — 'Shadow' is most active at dawn and dusk. Rangers locate her by tracking fresh territorial markings along the ridge.", Fact="Leopards are the strongest climbers of all big cats — capable of hauling prey twice their own body weight up into a tree.", Tags=new[]{"🌅 Dawn & dusk","🌲 Excellent climber","👻 Most elusive"}, StatA="Dawn & dusk", StatB="VU Status", StatC="Predator Ridge" },
                new AnimalData { Name="Spotted Hyena",      Species="Crocuta crocuta",            Emoji="🐕", Zone="Predator Ridge",   Category="Mammal",    Conservation="Least Concern",        ConsColor=Color.FromArgb(29,158,117),  PhotoFile="SpottedHyena.jpg",        Desc="Contrary to popular belief, hyenas are highly intelligent social hunters — not just scavengers. WildNest's pair demonstrates complex social bonding daily.", Fact="Female spotted hyenas are larger and more dominant than males — a rare example of female-led social hierarchy in mammals.", Tags=new[]{"🧠 Highly intelligent","👥 Social hunters","♀ Female dominant"}, StatA="Pair residents", StatB="Female dominant", StatC="Predator Ridge" },
                new AnimalData { Name="Sun Bear",           Species="Helarctos malayanus",        Emoji="🐻", Zone="Predator Ridge",   Category="Mammal",    Conservation="Vulnerable",           ConsColor=Color.FromArgb(230,130,30),  PhotoFile="SunBear.jpg",             Desc="The world's smallest bear — 'Sol' and 'Luna' were confiscated from illegal wildlife traders in 2019. They now thrive in WildNest's forest ridge habitat.", Fact="Sun bears have tongues up to 25cm long — perfectly evolved to extract honey and insects from deep inside tree crevices.", Tags=new[]{"👅 25cm tongue","🏥 Rescued 2019","🐝 Honey specialist"}, StatA="25cm tongue", StatB="Rescued 2019", StatC="Predator Ridge" },
                new AnimalData { Name="Visayan Warty Pig",  Species="Sus cebifrons",              Emoji="🐗", Zone="Conservation Hub", Category="Mammal",    Conservation="Critically Endangered",ConsColor=Color.FromArgb(180,20,20),   PhotoFile="VisayanWartyPig.jpg",     Desc="One of the rarest pigs in the world — endemic to the Visayas islands. WildNest's breeding pair has produced three litters, significantly aiding species survival.", Fact="Male Visayan Warty Pigs grow a dramatic mane of long hair during breeding season — looking strikingly different from females.", Tags=new[]{"🇵🇭 Visayas endemic","🥚 3 litters bred","⚠ Critically rare"}, StatA="3 litters bred", StatB="CR Status", StatC="Conservation Hub" },
                new AnimalData { Name="Palawan Bearcat",    Species="Arctictis binturong",        Emoji="🐻", Zone="Conservation Hub", Category="Mammal",    Conservation="Vulnerable",           ConsColor=Color.FromArgb(230,130,30),  PhotoFile="PalawanBearcat.jpg",      Desc="The binturong is neither a bear nor a cat. This unusual animal smells strongly of buttered popcorn due to a chemical compound in its scent glands.", Fact="Binturongs smell like popcorn because their urine contains 2-acetyl-1-pyrroline — the same molecule that gives popcorn its aroma.", Tags=new[]{"🍿 Smells like popcorn","🌙 Arboreal","🐾 Neither bear nor cat"}, StatA="Popcorn scent", StatB="VU Status", StatC="Conservation Hub" },
                new AnimalData { Name="Tamaraw",            Species="Bubalus mindorensis",        Emoji="🐃", Zone="Conservation Hub", Category="Mammal",    Conservation="Critically Endangered",ConsColor=Color.FromArgb(180,20,20),   PhotoFile="Tamaraw.jpg",             Desc="Found nowhere else on Earth but Mindoro, Philippines. WildNest's resident 'Lakas' is part of a last-resort captive safety population for the species.", Fact="Only around 600 Tamaraw remain in the world — making it one of the rarest large mammals in Southeast Asia.", Tags=new[]{"🇵🇭 Mindoro only","⚠ 600 left worldwide","🌿 CR priority"}, StatA="600 left worldwide", StatB="CR Status", StatC="Conservation Hub" },
                new AnimalData { Name="Wildebeest",         Species="Connochaetes taurinus",      Emoji="🐃", Zone="Golden Savanna",   Category="Mammal",    Conservation="Least Concern",        ConsColor=Color.FromArgb(29,158,117),  PhotoFile="Wildebeest.jpg",          Desc="A herd of five blue wildebeest grazes the southern savanna year-round — often alongside the zebra herd in a natural mixed-species grazing pattern.", Fact="Wildebeest calves can stand and run within 7 minutes of being born — one of the fastest development rates of any land mammal.", Tags=new[]{"🏃 Runs at birth","🌿 Herd of 5","🦓 Grazes with zebras"}, StatA="Herd of 5", StatB="7min to run", StatC="Golden Savanna" },
                new AnimalData { Name="Philippine Eagle",   Species="Pithecophaga jefferyi",      Emoji="🦅", Zone="Aviary Dome",      Category="Bird",      Conservation="Critically Endangered",ConsColor=Color.FromArgb(180,20,20),   PhotoFile="PhilippineEagle.jpg",     Desc="The national bird of the Philippines — and WildNest's most critically protected resident. 'Bagwis' was rehabilitated after a wing injury in 2018 and cannot be released.", Fact="The Philippine Eagle has a wingspan of up to 2 metres and is one of the largest, most powerful raptors in the world.", Tags=new[]{"🇵🇭 National Bird","🏥 Rescued 2018","⚠ CR Species"}, StatA="2m wingspan", StatB="Rescued 2018", StatC="Aviary Dome" },
                new AnimalData { Name="Rhinoceros Hornbill",Species="Buceros rhinoceros",         Emoji="🦜", Zone="Aviary Dome",      Category="Bird",      Conservation="Vulnerable",           ConsColor=Color.FromArgb(230,130,30),  PhotoFile="RhinocerosHornbill.jpg",  Desc="Named for the dramatic golden casque atop its bill — a keystone of Southeast Asian rainforests. WildNest houses a breeding pair actively participating in conservation.", Fact="The hornbill's casque is hollow — it acts as a resonating chamber that amplifies the bird's booming call across the forest canopy.", Tags=new[]{"🔊 Loud calls","🥚 Breeding pair","🌿 Canopy bird"}, StatA="Breeding pair", StatB="VU Status", StatC="Aviary Dome" },
                new AnimalData { Name="Scarlet Macaw",      Species="Ara macao",                  Emoji="🦜", Zone="Aviary Dome",      Category="Bird",      Conservation="Least Concern",        ConsColor=Color.FromArgb(29,158,117),  PhotoFile="ScarletMacaw.jpg",        Desc="WildNest's most colourful residents — a flock of seven macaws that interact freely with guests in the walk-through aviary zone every morning.", Fact="Macaws can mimic human speech and use it socially — they are one of the few animals that learn vocal patterns from their peers.", Tags=new[]{"🗣 Can mimic speech","🌈 Flock of 7","✋ Guest interactive"}, StatA="Flock of 7", StatB="Guest interactive", StatC="Aviary Dome" },
                new AnimalData { Name="Philippine Cockatoo",Species="Cacatua haematuropygia",     Emoji="🦚", Zone="Aviary Dome",      Category="Bird",      Conservation="Critically Endangered",ConsColor=Color.FromArgb(180,20,20),   PhotoFile="PhilippineCockatoo.jpg",  Desc="Known locally as 'Katala' — this snow-white parrot is one of the Philippines' most endangered birds. WildNest participates in the national breeding program.", Fact="Philippine Cockatoos mate for life — bonded pairs are rarely more than arm's length apart, even during flight.", Tags=new[]{"🤍 Snow white","💑 Mates for life","🇵🇭 Endemic"}, StatA="Mates for life", StatB="CR Status", StatC="Aviary Dome" },
                new AnimalData { Name="Pelican",            Species="Pelecanus conspicillatus",   Emoji="🦢", Zone="Aquatic Zone",     Category="Bird",      Conservation="Least Concern",        ConsColor=Color.FromArgb(29,158,117),  PhotoFile="Pelican.jpg",             Desc="A pair of Australian Pelicans glides across WildNest's main lake — the white plumage and enormous bill make them among the most photographed residents.", Fact="A pelican's bill pouch can hold up to 13 litres of water — three times more than its stomach — used as a net to scoop fish.", Tags=new[]{"📸 Most photographed","🌊 Lake resident","🐟 Scoop-net hunter"}, StatA="Bill holds 13L", StatB="Lake resident", StatC="Aquatic Zone" },
                new AnimalData { Name="River Otter",        Species="Lutra lutra",                Emoji="🦦", Zone="Aquatic Zone",     Category="Aquatic",   Conservation="Near Threatened",      ConsColor=Color.FromArgb(212,160,23),  PhotoFile="RiverOtter.jpg",          Desc="A family of four otters lives along the main channel of the Aquatic Zone. They are most active at dawn — spending hours playing, grooming, and hunting.", Fact="Otters hold hands while sleeping to prevent drifting apart — this behaviour, called 'rafting', keeps bonded pairs together.", Tags=new[]{"🌅 Dawn active","👐 Holds hands","🐟 Skilled hunters"}, StatA="Family of 4", StatB="NT Status", StatC="Aquatic Zone" },
                new AnimalData { Name="Hippopotamus",       Species="Hippopotamus amphibius",     Emoji="🦛", Zone="Aquatic Zone",     Category="Aquatic",   Conservation="Vulnerable",           ConsColor=Color.FromArgb(230,130,30),  PhotoFile="Hippopotamus.jpg",        Desc="'Bruno' is WildNest's resident hippo — weighing in at 1,800kg. Despite peaceful appearances, hippos are among the most dangerous animals in Africa.", Fact="Hippos secrete a reddish fluid called 'blood sweat' — it's actually a natural sunscreen and antimicrobial agent, not blood at all.", Tags=new[]{"⚠ Most dangerous","🌊 Semi-aquatic","⚖ 1,800kg"}, StatA="1,800kg", StatB="VU Status", StatC="Aquatic Zone" },
                new AnimalData { Name="Green Sea Turtle",   Species="Chelonia mydas",             Emoji="🐢", Zone="Aquatic Zone",     Category="Aquatic",   Conservation="Endangered",           ConsColor=Color.FromArgb(210,70,30),   PhotoFile="GreenSeaTurtle.jpg",      Desc="WildNest's two sea turtles were rescued from fishing nets — permanent residents unable to survive in the open ocean after their injuries.", Fact="Sea turtles navigate using the Earth's magnetic field — they can return to the exact beach where they were born decades later.", Tags=new[]{"🧭 Magnetic nav","🏥 Rescued from nets","🌊 2 residents"}, StatA="2 residents", StatB="EN Status", StatC="Aquatic Zone" },
                new AnimalData { Name="Dugong",             Species="Dugong dugon",               Emoji="🐬", Zone="Conservation Hub", Category="Aquatic",   Conservation="Vulnerable",           ConsColor=Color.FromArgb(230,130,30),  PhotoFile="Dugong.jpg",              Desc="WildNest's rarest aquatic resident — one of only a handful of dugongs in captivity globally. 'Perlas' was rescued after stranding on a Carmen beach in 2020.", Fact="Dugongs are believed to be the origin of mermaid legends — ancient sailors mistook them for half-human creatures from a distance.", Tags=new[]{"🧜 Mermaid legend","🏥 Beach rescue 2020","🌊 Rare in captivity"}, StatA="Rare in captivity", StatB="VU Status", StatC="Conservation Hub" },
                new AnimalData { Name="Giant Catfish",      Species="Pangasianodon gigas",        Emoji="🐟", Zone="Aquatic Zone",     Category="Aquatic",   Conservation="Critically Endangered",ConsColor=Color.FromArgb(180,20,20),   PhotoFile="GiantCatfish.jpg",        Desc="One of the world's largest freshwater fish — can reach 3 metres and 300kg. WildNest's juvenile resident is already 1.4 metres long.", Fact="The Mekong Giant Catfish has no teeth and feeds entirely on algae and plankton — despite being one of the largest freshwater fish alive.", Tags=new[]{"📏 Up to 3m","🌿 Algae feeder","⚠ CR Species"}, StatA="Up to 3m long", StatB="CR Status", StatC="Aquatic Zone" },
                new AnimalData { Name="Freshwater Crocodile",Species="Crocodylus johnstoni",     Emoji="🐊", Zone="Aquatic Zone",     Category="Reptile",   Conservation="Least Concern",        ConsColor=Color.FromArgb(29,158,117),  PhotoFile="FreshwaterCrocodile.jpg", Desc="Three freshwater crocodiles inhabit the secured eastern channel. Viewing is from an elevated glass-floor platform — one of WildNest's most thrilling experiences.", Fact="Crocodiles have the strongest bite force of any animal on Earth — but the muscles to open their jaw are so weak a rubber band can keep them shut.", Tags=new[]{"🔒 Glass floor view","💪 Strongest bite","🦎 3 residents"}, StatA="3 residents", StatB="Glass floor view", StatC="Aquatic Zone" },
                new AnimalData { Name="Philippine Crocodile",Species="Crocodylus mindorensis",   Emoji="🐊", Zone="Aquatic Zone",     Category="Reptile",   Conservation="Critically Endangered",ConsColor=Color.FromArgb(180,20,20),   PhotoFile="PhilippineCrocodile.jpg", Desc="One of the rarest crocodilians on Earth — only ~250 remain in the wild. WildNest's breeding program has produced 14 hatchlings since 2015.", Fact="The Philippine Crocodile is a freshwater species far smaller than its saltwater cousin — adults rarely exceed 3 metres.", Tags=new[]{"🇵🇭 Endemic","🥚 14 hatchlings","⚠ 250 left wild"}, StatA="14 hatchlings bred", StatB="CR Status", StatC="Aquatic Zone" },
                new AnimalData { Name="Burmese Python",     Species="Python bivittatus",          Emoji="🐍", Zone="Reptile Zone",     Category="Reptile",   Conservation="Vulnerable",           ConsColor=Color.FromArgb(230,130,30),  PhotoFile="BurmesePython.jpg",       Desc="At 5.2 metres and 75kg, 'Goliath' is WildNest's largest snake. Ranger-supervised viewing only — guests watch scheduled feeding from an observation gallery.", Fact="Pythons can go an entire year without eating after consuming a large meal — their metabolism slows dramatically to conserve energy.", Tags=new[]{"📏 5.2m / 75kg","🔒 Supervised viewing","🍽 Scheduled feeding"}, StatA="5.2m long", StatB="Supervised only", StatC="Reptile Zone" },
                new AnimalData { Name="Monitor Lizard",     Species="Varanus salvator",           Emoji="🦎", Zone="Reptile Zone",     Category="Reptile",   Conservation="Least Concern",        ConsColor=Color.FromArgb(29,158,117),  PhotoFile="MonitorLizard.jpg",       Desc="Southeast Asia's largest lizard — WildNest's resident pair bask on rocks near the reptile zone waterway and are reliably spotted by midmorning.", Fact="Monitor lizards have forked tongues that function like a chemical sensor — they can detect prey from over a kilometre away.", Tags=new[]{"📏 Up to 3m","👅 Chemical sensor","🌿 2 residents"}, StatA="2 residents", StatB="LC Status", StatC="Reptile Zone" },
                new AnimalData { Name="Giant Gecko",        Species="Gekko gecko",                Emoji="🦎", Zone="Reptile Zone",     Category="Reptile",   Conservation="Least Concern",        ConsColor=Color.FromArgb(29,158,117),  PhotoFile="GiantGecko.jpg",          Desc="The Tokay Gecko's loud, distinctive 'to-KAY' call is one of WildNest's most recognisable night sounds — often heard before the animal is seen.", Fact="Gecko feet are covered in millions of microscopic hairs that create a van der Waals molecular attraction — they can walk upside down on glass.", Tags=new[]{"🔊 Loud night call","🦶 Walks on glass","🌙 Nocturnal"}, StatA="Walks on glass", StatB="Night active", StatC="Reptile Zone" },
                new AnimalData { Name="Philippine Tarsier", Species="Carlito syrichta",           Emoji="🐸", Zone="Nocturnal Trail",  Category="Nocturnal", Conservation="Near Threatened",      ConsColor=Color.FromArgb(212,160,23),  PhotoFile="PhilippineTarsier.jpg",   Desc="One of the world's smallest primates — the tarsier's eyes are fixed in its skull, so it rotates its entire head 180° to look sideways.", Fact="Each of the tarsier's eyes is as large as its entire brain — the largest eyes relative to body size of any mammal on Earth.", Tags=new[]{"👁 Eyes = brain size","🌙 Nocturnal only","🇵🇭 Philippine endemic"}, StatA="Eyes = brain size", StatB="NT Status", StatC="Nocturnal Trail" },
                new AnimalData { Name="Philippine Owl",     Species="Ninox philippensis",         Emoji="🦉", Zone="Nocturnal Trail",  Category="Nocturnal", Conservation="Least Concern",        ConsColor=Color.FromArgb(29,158,117),  PhotoFile="PhilippineOwl.jpg",       Desc="The silent hunter of WildNest's nocturnal trail — rangers report sightings almost every Night Safari, perched along the bioluminescent pool pathway.", Fact="Owls have asymmetrical ears — one higher than the other — allowing them to pinpoint sound in three dimensions with extraordinary accuracy.", Tags=new[]{"🌙 Night Safari star","👂 3D hearing","🔦 Torch-spottable"}, StatA="Night Safari staple", StatB="LC Status", StatC="Nocturnal Trail" },
                new AnimalData { Name="Giant Bat",          Species="Pteropus vampyrus",          Emoji="🦇", Zone="Nocturnal Trail",  Category="Nocturnal", Conservation="Near Threatened",      ConsColor=Color.FromArgb(212,160,23),  PhotoFile="GiantBat.jpg",            Desc="With a wingspan reaching 1.7 metres, the large flying fox is the world's biggest bat. A colony of 40+ roosts in the nocturnal zone's banyan cluster.", Fact="Flying foxes navigate by sight and smell rather than echolocation — they have better eyesight than most humans.", Tags=new[]{"🌙 Colony of 40+","📏 1.7m wingspan","🌸 Pollinators"}, StatA="Colony of 40+", StatB="1.7m wingspan", StatC="Nocturnal Trail" },
                new AnimalData { Name="Civet Cat",          Species="Viverra zibetha",            Emoji="🐈", Zone="Nocturnal Trail",  Category="Nocturnal", Conservation="Least Concern",        ConsColor=Color.FromArgb(29,158,117),  PhotoFile="CivetCat.jpg",            Desc="The Asian civet is a solitary nocturnal predator with a distinctive patterned body. Guests frequently spot them on the night trail without any ranger assistance.", Fact="Civet cats are responsible for producing Kopi Luwak — one of the world's most expensive coffees — through their unique digestive process.", Tags=new[]{"☕ Kopi Luwak link","🌙 Solitary hunter","👁 Easy night spot"}, StatA="Kopi Luwak link", StatB="LC Status", StatC="Nocturnal Trail" },
                new AnimalData { Name="Slow Loris",         Species="Nycticebus coucang",         Emoji="🦥", Zone="Jungle Canopy",    Category="Nocturnal", Conservation="Endangered",           ConsColor=Color.FromArgb(210,70,30),   PhotoFile="SlowLoris.jpg",           Desc="The only venomous primate in the world — the slow loris secretes toxin from glands on its arms, mixed with saliva to deliver a toxic bite when threatened.", Fact="Slow lorises are sold illegally in the pet trade — but their viral 'cute' videos hide extreme suffering. WildNest's resident was confiscated from a trafficker.", Tags=new[]{"⚠ Only venomous primate","🏥 Trafficking rescue","🌙 Nocturnal"}, StatA="Only venomous primate", StatB="EN Status", StatC="Jungle Canopy" },
                new AnimalData { Name="Spider Monkey",      Species="Ateles geoffroyi",           Emoji="🐒", Zone="Jungle Canopy",    Category="Primate",   Conservation="Endangered",           ConsColor=Color.FromArgb(210,70,30),   PhotoFile="SpiderMonkey.jpg",        Desc="A troop of eight spider monkeys inhabits the upper canopy walkway. They use their prehensile tails as a fifth limb — capable of supporting their full bodyweight.", Fact="Spider monkeys are essential seed dispersers — they can travel 10km a day and deposit seeds across vast areas of rainforest.", Tags=new[]{"🌿 Troop of 8","🌲 Canopy dwellers","🐾 Prehensile tail"}, StatA="Troop of 8", StatB="EN Status", StatC="Jungle Canopy" },
                new AnimalData { Name="White-handed Gibbon",Species="Hylobates lar",              Emoji="🦧", Zone="Jungle Canopy",    Category="Primate",   Conservation="Endangered",           ConsColor=Color.FromArgb(210,70,30),   PhotoFile="WhiteHandedGibbon.jpg",   Desc="The fastest-moving primate in the trees — gibbons brachiate at up to 55km/h through the Jungle Canopy zone, thrilling guests on the walkway.", Fact="Gibbons are monogamous and sing duets with their partners every morning — these 'love songs' can last up to 30 minutes.", Tags=new[]{"🎵 Morning duets","💨 55km/h swinging","💑 Monogamous"}, StatA="55km/h swinging", StatB="EN Status", StatC="Jungle Canopy" },
                new AnimalData { Name="Macaque",            Species="Macaca fascicularis",        Emoji="🐒", Zone="Jungle Canopy",    Category="Primate",   Conservation="Least Concern",        ConsColor=Color.FromArgb(29,158,117),  PhotoFile="Macaque.jpg",             Desc="A troop of 12 long-tailed macaques is WildNest's most social — and most mischievous — residents. Frequently spotted near the canopy café stealing fruit.", Fact="Macaques are one of the few non-human animals that wash their food before eating — and they teach this behaviour to their young.", Tags=new[]{"😅 Mischievous","🍎 Food washers","👶 Teach young"}, StatA="Troop of 12", StatB="Food washers", StatC="Jungle Canopy" },
            };
        }

        // ════════════════════════════════════════════════════════════
        //  HELPER: load image from embedded Resources by filename
        // ════════════════════════════════════════════════════════════
        private static Image LoadPhoto(string photoFile)
        {
            try
            {
                return Project.AppAssetLoader.LoadAnimalPhoto(photoFile);
            }
            catch { return null; }
        }

        // ════════════════════════════════════════════════════════════
        //  STYLE ALL CONTROLS
        // ════════════════════════════════════════════════════════════
        private void StyleAll()
        {
            this.BackColor = C_BG2;

            // ── HERO PANEL ─────────────────────────────────────────
            // Everything (eyebrow, title, subtitle) is painted via GDI+ in PnlHero_Paint.
            // The Label controls are hidden — they exist only to satisfy the designer.
            pnlHero.BackColor = C_BG;
            pnlHero.Height = 340;
            pnlHero.Paint += PnlHero_Paint;

            // Hide all three hero text labels — we paint them ourselves
            lblLocation.Visible = false;
            lblHeroTitle.Visible = false;
            lblHeroSub.Visible = false;

            // Position the view-switcher TLP centered, below the painted text
            // TLP is 700px wide — center it on the panel
            Action layoutHero = () =>
            {
                int cx = pnlHero.Width / 2;
                tlpViewSwitcher.Size = new Size(700, 56);
                tlpViewSwitcher.Location = new Point(cx - 350, 268);
            };
            pnlHero.Resize += (s, e) => { layoutHero(); pnlHero.Invalidate(); };
            pnlHero.HandleCreated += (s, e) => layoutHero();

            // ── VIEW SWITCHER ──────────────────────────────────────
            // HTML: border-radius:14px on container, border-radius:10px on each button
            tlpViewSwitcher.BackColor = Color.Transparent;
            tlpViewSwitcher.Size = new Size(700, 56);
            tlpViewSwitcher.Padding = new Padding(5);
            tlpViewSwitcher.CellBorderStyle = TableLayoutPanelCellBorderStyle.None;
            tlpViewSwitcher.Paint += (s, pe) =>
            {
                var g = pe.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var r = tlpViewSwitcher.ClientRectangle;
                r.Inflate(-1, -1);
                // Rounded-rect with radius 14 — 4 corners only, straight edges between
                using (var path = MakeRoundedPath(r, 14))
                {
                    using (var fill = new SolidBrush(Color.FromArgb(10, 255, 255, 255)))
                        g.FillPath(fill, path);
                    using (var pen = new Pen(Color.FromArgb(64, 212, 160, 23), 1f))
                        g.DrawPath(pen, path);
                }
            };

            StyleViewBtn(btnSpotlight, "🔦   Featured Residents — Spotlight", true);
            StyleViewBtn(btnAllResidents, "🗂️   All Residents — Full Sanctuary", false);

            btnSpotlight.Click += (s, e) => SwitchView(true);
            btnAllResidents.Click += (s, e) => SwitchView(false);

            // ── STATUS BAR ────────────────────────────────────────
            pnlStatusBar.BackColor = Color.FromArgb(7, 20, 10);
            pnlStatusBar.Height = 60;
            pnlStatusBar.Paint += PnlStatusBar_Paint;
            tlpStatusBar.BackColor = Color.Transparent;

            lblCabinStatus.Text = "🦁  35 Resident Species";
            lblCabinStatus.ForeColor = C_GOLD;
            lblCabinStatus.BackColor = Color.Transparent;
            lblCabinStatus.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            lblCabinStatus.TextAlign = ContentAlignment.MiddleCenter;

            lblSafariTimer.Text = "🌿  8 Wildlife Zones";
            lblSafariTimer.ForeColor = C_GOLD;
            lblSafariTimer.BackColor = Color.Transparent;
            lblSafariTimer.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            lblSafariTimer.TextAlign = ContentAlignment.MiddleCenter;

            lblAnimalCount.Text = "🏥  312 Animals Rehabilitated";
            lblAnimalCount.ForeColor = C_GOLD;
            lblAnimalCount.BackColor = Color.Transparent;
            lblAnimalCount.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            lblAnimalCount.TextAlign = ContentAlignment.MiddleCenter;

            // ── CONTENT / PHOTO AREA ──────────────────────────────
            pnlContentArea.BackColor = C_BG2;
            pnlPhotoArea.BackColor = C_BG2;
            picAnimalPhoto.BackColor = C_BG2;
            picAnimalPhoto.Paint += PicAnimalPhoto_Paint;

            // Info labels over the photo
            lblAnimalCounter.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            lblAnimalCounter.ForeColor = Color.FromArgb(180, 212, 160, 23);
            lblAnimalCounter.BackColor = Color.Transparent;
            lblAnimalCounter.Location = new Point(48, 28);

            lblConservationStatus.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            lblConservationStatus.BackColor = Color.Transparent;
            // Right-align: will be updated each LoadSpotlight

            lblZoneBadge.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            lblZoneBadge.ForeColor = C_GOLD;
            lblZoneBadge.BackColor = Color.Transparent;
            lblZoneBadge.Location = new Point(48, 50);

            lblAnimalName.Font = new Font("Georgia", 52f, FontStyle.Bold);
            lblAnimalName.ForeColor = C_CREAM;
            lblAnimalName.BackColor = Color.Transparent;
            lblAnimalName.AutoSize = true;
            lblAnimalName.MaximumSize = new Size(700, 0);
            lblAnimalName.Location = new Point(48, 82);

            lblSpecies.Font = new Font("Segoe UI", 13f, FontStyle.Italic);
            lblSpecies.ForeColor = Color.FromArgb(120, 248, 244, 239);
            lblSpecies.BackColor = Color.Transparent;
            lblSpecies.AutoSize = true;

            lblDescription.Font = new Font("Segoe UI", 11f);
            lblDescription.ForeColor = Color.FromArgb(200, 248, 244, 239);
            lblDescription.BackColor = Color.Transparent;
            lblDescription.AutoSize = false;
            lblDescription.Size = new Size(680, 68);

            flpStatPills.BackColor = Color.Transparent;
            flpStatPills.WrapContents = false;

            pnlFactBox.BackColor = Color.Transparent;
            pnlFactBox.Paint += PnlFactBox_Paint;
            pnlFactBox.Size = new Size(680, 110);

            flpTags.BackColor = Color.Transparent;
            flpTags.Size = new Size(680, 40);
            flpTags.WrapContents = false;

            // ── ACTION BAR ────────────────────────────────────────
            pnlActionBar.BackColor = Color.FromArgb(245, 2, 10, 4);
            pnlActionBar.Paint += PnlActionBar_Paint;
            pnlActionBar.Height = 90;

            lblUpNext.Text = "UP NEXT";
            lblUpNext.Font = new Font("Segoe UI", 8f, FontStyle.Bold);
            lblUpNext.ForeColor = Color.FromArgb(140, 212, 160, 23);
            lblUpNext.BackColor = Color.Transparent;
            lblUpNext.Location = new Point(100, 20);

            lblNextAnimalName.Font = new Font("Segoe UI", 13f, FontStyle.Bold);
            lblNextAnimalName.ForeColor = C_CREAM;
            lblNextAnimalName.BackColor = Color.Transparent;
            lblNextAnimalName.AutoSize = true;
            lblNextAnimalName.Location = new Point(100, 38);

            picNextAnimal.SizeMode = PictureBoxSizeMode.Zoom;
            picNextAnimal.BackColor = Color.FromArgb(20, 40, 24);
            picNextAnimal.Size = new Size(62, 62);
            picNextAnimal.Location = new Point(22, 14);

            pnlActionBar.BringToFront();
            btnSpotlightNext.BringToFront();
            StyleSpotlightBtn();
            btnSpotlightNext.Click += (s, e) => AdvanceAnimal();

            // ── QUEUE PANEL ───────────────────────────────────────
            pnlQueue.BackColor = C_QUEUE_BG;
            pnlQueue.Width = 380;
            pnlQueue.Paint += PnlQueue_Paint;

            lblQueueTitle.Text = "ANIMAL QUEUE";
            lblQueueTitle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            lblQueueTitle.ForeColor = C_GOLD;
            lblQueueTitle.BackColor = Color.Transparent;
            lblQueueTitle.Location = new Point(20, 18);

            lblQueueSub.Text = "All 35 residents — cycling endlessly";
            lblQueueSub.Font = new Font("Segoe UI", 8.5f);
            lblQueueSub.ForeColor = Color.FromArgb(80, 248, 244, 239);
            lblQueueSub.BackColor = Color.Transparent;
            lblQueueSub.AutoSize = true;
            lblQueueSub.Location = new Point(20, 38);

            pbQueueProgress.Visible = false;

            lblQueueCount.Text = $"1 / {(_animals?.Count ?? 35)}";
            lblQueueCount.Font = new Font("Segoe UI", 8f, FontStyle.Bold);
            lblQueueCount.ForeColor = Color.FromArgb(150, 212, 160, 23);
            lblQueueCount.BackColor = Color.Transparent;
            lblQueueCount.AutoSize = true;
            lblQueueCount.Location = new Point(310, 58);

            flpQueueList.BackColor = Color.Transparent;
            flpQueueList.FlowDirection = FlowDirection.TopDown;
            flpQueueList.WrapContents = false;
            flpQueueList.AutoScroll = false;
            flpQueueList.Dock = DockStyle.None;
            flpQueueList.Location = new Point(0, 80);
            flpQueueList.Size = new Size(380, pnlQueue.Height - 80);
        }

        // ════════════════════════════════════════════════════════════
        //  STYLE HELPERS
        // ════════════════════════════════════════════════════════════
        private void StyleViewBtn(Button btn, string text, bool active)
        {
            btn.Text = text;
            btn.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.Cursor = Cursors.Hand;
            btn.Dock = DockStyle.Fill;
            btn.Margin = new Padding(4);
            btn.BackColor = Color.Transparent;
            btn.ForeColor = Color.Transparent;
            btn.Tag = active;

            btn.Paint -= ViewBtn_Paint;
            btn.Paint += ViewBtn_Paint;
        }

        private void ViewBtn_Paint(object sender, PaintEventArgs e)
        {
            var btn = (Button)sender;
            bool active = btn.Tag is bool b && b;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            var r = btn.ClientRectangle;
            r.Inflate(-2, -2);

            using (var path = MakeRoundedPath(r, 10))
            {
                if (active)
                    using (var fill = new SolidBrush(C_GOLD))
                        g.FillPath(fill, path);
                else
                {
                    // Hover: faint light bg; inactive default: transparent
                }
            }

            TextRenderer.DrawText(g, btn.Text, btn.Font, btn.ClientRectangle,
                active ? Color.FromArgb(7, 26, 14) : Color.FromArgb(130, 248, 244, 239),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private void StyleSpotlightBtn()
        {
            btnSpotlightNext.Text = "  Spotlight Next Animal  →  ";
            btnSpotlightNext.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            btnSpotlightNext.FlatStyle = FlatStyle.Flat;
            btnSpotlightNext.FlatAppearance.BorderSize = 0;
            btnSpotlightNext.BackColor = Color.Transparent;
            btnSpotlightNext.ForeColor = Color.Transparent;
            btnSpotlightNext.Cursor = Cursors.Hand;
            btnSpotlightNext.Size = new Size(290, 54);
            btnSpotlightNext.Anchor = AnchorStyles.None; // NO auto-anchor — we position manually only

            // Default position assuming standard 1262px form (882px photo area)
            // This makes it visible immediately before any layout event fires
            btnSpotlightNext.Location = new Point(532, 18);

            btnSpotlightNext.Paint -= SpotlightBtn_Paint;
            btnSpotlightNext.Paint += SpotlightBtn_Paint;

            // Hook EVERY relevant event to guarantee repositioning
            pnlActionBar.Resize += (s, e) => PositionSpotlightBtn();
            pnlActionBar.Layout += (s, e) => PositionSpotlightBtn();
            pnlPhotoArea.Resize += (s, e) => PositionSpotlightBtn();
            pnlPhotoArea.Layout += (s, e) => PositionSpotlightBtn();
            pnlPhotoArea.VisibleChanged += (s, e) =>
            {
                if (pnlPhotoArea.Visible)
                    BeginInvoke(new Action(PositionSpotlightBtn));
            };
        }

        private void PositionSpotlightBtn()
        {
            if (pnlActionBar.Width < 100) return;

            // Subtract queue panel width so button doesn't hide behind it
            int usableWidth = pnlActionBar.Width - (pnlQueue.Visible ? pnlQueue.Width : 0);
            int bx = usableWidth - btnSpotlightNext.Width - 30;
            int by = (pnlActionBar.Height - btnSpotlightNext.Height) / 2;
            if (by < 0) by = 18;

            if (btnSpotlightNext.Location.X != bx || btnSpotlightNext.Location.Y != by)
                btnSpotlightNext.Location = new Point(bx, by);
        }

        private void SpotlightBtn_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            var r = btnSpotlightNext.ClientRectangle;
            r.Inflate(-1, -1);

            using (var path = MakeRoundedPath(r, 14))
            {
                using (var fill = new SolidBrush(C_GOLD))
                    g.FillPath(fill, path);
                using (var pen = new Pen(Color.FromArgb(80, 255, 230, 120), 1.5f))
                    g.DrawPath(pen, path);
            }

            TextRenderer.DrawText(g, btnSpotlightNext.Text,
                new Font("Segoe UI", 11f, FontStyle.Bold),
                btnSpotlightNext.ClientRectangle,
                Color.FromArgb(7, 26, 14),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        // ════════════════════════════════════════════════════════════
        //  PAINT EVENTS
        // ════════════════════════════════════════════════════════════
        private void PnlHero_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            var r = pnlHero.ClientRectangle;
            if (r.Width <= 0 || r.Height <= 0)
                return;

            HeroSurfacePainter.Paint(g, r, HeroSurfaceVariant.Animals);

            int cx = r.Width / 2;

            // ── Eyebrow: "── CARMEN, CEBU — PHILIPPINES ──" ──────
            // Draw decorative lines + text centered
            string eyebrow = "CARMEN, CEBU  —  PHILIPPINES";
            var eyebrowFont = new Font("Segoe UI", 9f, FontStyle.Bold);
            var eyebrowSize = TextRenderer.MeasureText(g, eyebrow, eyebrowFont);
            int eyebrowY = 44;
            int textX = cx - eyebrowSize.Width / 2;
            int lineLen = 28;
            int lineGap = 10;

            // Left line
            using (var pen = new Pen(C_GOLD, 1f))
            {
                int lineY = eyebrowY + eyebrowSize.Height / 2;
                g.DrawLine(pen, textX - lineGap - lineLen, lineY, textX - lineGap, lineY);
                g.DrawLine(pen, textX + eyebrowSize.Width + lineGap, lineY,
                                textX + eyebrowSize.Width + lineGap + lineLen, lineY);
            }
            TextRenderer.DrawText(g, eyebrow, eyebrowFont,
                new Point(textX, eyebrowY), C_GOLD);

            // ── Hero title: "Meet the Wild Residents" ────────────
            // "Wild" is drawn in gold, rest in cream
            // We need to measure parts individually to position them inline
            var titleFont = new Font("Georgia", 46f, FontStyle.Bold);
            string part1 = "Meet the ";
            string part2 = "Wild";
            string part3 = " Residents";

            var s1 = TextRenderer.MeasureText(g, part1, titleFont,
                         new Size(9999, 999), TextFormatFlags.NoPadding);
            var s2 = TextRenderer.MeasureText(g, part2, titleFont,
                         new Size(9999, 999), TextFormatFlags.NoPadding);
            var s3 = TextRenderer.MeasureText(g, part3, titleFont,
                         new Size(9999, 999), TextFormatFlags.NoPadding);

            int totalTitleW = s1.Width + s2.Width + s3.Width;
            int titleY = 84;
            int tx = cx - totalTitleW / 2;

            TextRenderer.DrawText(g, part1, titleFont,
                new Point(tx, titleY), C_CREAM,
                TextFormatFlags.NoPadding);
            TextRenderer.DrawText(g, part2, titleFont,
                new Point(tx + s1.Width, titleY), C_GOLD,
                TextFormatFlags.NoPadding);
            TextRenderer.DrawText(g, part3, titleFont,
                new Point(tx + s1.Width + s2.Width, titleY), C_CREAM,
                TextFormatFlags.NoPadding);

            // ── Subtitle ─────────────────────────────────────────
            string sub = "35 remarkable species roaming freely across 170 hectares of restored sanctuary — each with a name, a story, and a dedicated ranger.";
            var subFont = new Font("Segoe UI", 10.5f);
            var subColor = Color.FromArgb(128, 248, 244, 239);
            int subW = 480;
            int subY = titleY + s1.Height + 10;

            TextRenderer.DrawText(g, sub, subFont,
                new Rectangle(cx - subW / 2, subY, subW, 90),
                subColor,
                TextFormatFlags.WordBreak | TextFormatFlags.HorizontalCenter);

            // ── Bottom border line ────────────────────────────────
            using (var pen = new Pen(Color.FromArgb(40, 212, 160, 23), 1))
                g.DrawLine(pen, 0, r.Bottom - 1, r.Width, r.Bottom - 1);

            eyebrowFont.Dispose();
            titleFont.Dispose();
            subFont.Dispose();
        }

        private void PnlStatusBar_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var r = pnlStatusBar.ClientRectangle;
            if (r.Width <= 0 || r.Height <= 0)
                return;

            using (var br = new LinearGradientBrush(r, Color.FromArgb(8, 24, 12), Color.FromArgb(5, 16, 8), LinearGradientMode.Vertical))
                g.FillRectangle(br, r);
            using (var pen = new Pen(Color.FromArgb(50, 212, 160, 23), 1))
            {
                g.DrawLine(pen, 0, 0, r.Width, 0);
                g.DrawLine(pen, 0, r.Bottom - 1, r.Width, r.Bottom - 1);
            }
            int col = r.Width / 3;
            using (var pen = new Pen(Color.FromArgb(30, 212, 160, 23), 1))
            {
                g.DrawLine(pen, col, 12, col, r.Bottom - 12);
                g.DrawLine(pen, col * 2, 12, col * 2, r.Bottom - 12);
            }
        }

        private void PicAnimalPhoto_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            var r = picAnimalPhoto.ClientRectangle;
            if (r.Width <= 1 || r.Height <= 1)
                return;

            // Draw photo full-panel
            if (_currentPhoto != null)
                g.DrawImage(_currentPhoto, r);
            else
                g.Clear(C_BG2);

            // Gradient vignettes to blend into dark UI
            using (var br = new LinearGradientBrush(new Rectangle(0, 0, r.Width, r.Height / 2),
                Color.FromArgb(180, 2, 10, 4), Color.Transparent, LinearGradientMode.Vertical))
                g.FillRectangle(br, 0, 0, r.Width, r.Height / 2);

            using (var br = new LinearGradientBrush(new Rectangle(0, r.Height / 3, r.Width, r.Height * 2 / 3),
                Color.Transparent, Color.FromArgb(250, 2, 10, 4), LinearGradientMode.Vertical))
                g.FillRectangle(br, 0, r.Height / 3, r.Width, r.Height * 2 / 3);

            using (var br = new LinearGradientBrush(new Rectangle(0, 0, r.Width / 2, r.Height),
                Color.FromArgb(140, 2, 10, 4), Color.Transparent, LinearGradientMode.Horizontal))
                g.FillRectangle(br, 0, 0, r.Width / 2, r.Height);

            // Gold accent line under species
            int lineY = lblSpecies.Bottom + 14;
            using (var pen = new Pen(C_GOLD, 2.5f))
                g.DrawLine(pen, 48, lineY, 110, lineY);
        }

        private void PnlFactBox_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var r = pnlFactBox.ClientRectangle;
            r.Inflate(-1, -1);
            using (var path = MakeRoundedPath(r, 12))
            {
                using (var fill = new SolidBrush(Color.FromArgb(160, 0, 0, 0)))
                    g.FillPath(fill, path);
                using (var pen = new Pen(Color.FromArgb(55, 212, 160, 23), 1))
                    g.DrawPath(pen, path);
            }
            using (var br = new SolidBrush(C_GOLD))
                g.FillRectangle(br, r.X, r.Y, 3, r.Height);

            TextRenderer.DrawText(g, "WILD FACT",
                new Font("Segoe UI", 7.5f, FontStyle.Bold),
                new Rectangle(r.X + 14, r.Y + 10, 120, 18),
                Color.FromArgb(180, 212, 160, 23),
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

            if (_animals != null && _currentIndex < _animals.Count)
                TextRenderer.DrawText(g, _animals[_currentIndex].Fact,
                    new Font("Segoe UI", 10f),
                    new Rectangle(r.X + 14, r.Y + 30, r.Width - 24, r.Height - 36),
                    Color.FromArgb(210, 248, 244, 239),
                    TextFormatFlags.Left | TextFormatFlags.WordBreak);
        }

        private void PnlActionBar_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            var r = pnlActionBar.ClientRectangle;
            if (r.Width <= 0 || r.Height <= 0)
                return;

            using (var br = new LinearGradientBrush(r, Color.FromArgb(250, 2, 10, 4), Color.FromArgb(240, 4, 14, 7), LinearGradientMode.Vertical))
                g.FillRectangle(br, r);
            using (var pen = new Pen(Color.FromArgb(50, 212, 160, 23), 1))
                g.DrawLine(pen, 0, 0, r.Width, 0);
        }

        private void PnlQueue_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var r = pnlQueue.ClientRectangle;
            if (r.Width <= 0 || r.Height <= 0)
                return;

            using (var br = new LinearGradientBrush(r, Color.FromArgb(6, 20, 10), Color.FromArgb(2, 8, 4), LinearGradientMode.Vertical))
                g.FillRectangle(br, r);
            using (var pen = new Pen(Color.FromArgb(40, 212, 160, 23), 1))
                g.DrawLine(pen, 0, 0, 0, r.Height);

            // Progress bar drawn in paint
            int barY = 66, barW = 330;
            using (var br = new SolidBrush(Color.FromArgb(30, 255, 255, 255)))
                g.FillRectangle(br, 20, barY, barW, 3);
            float pct = _animals != null ? (float)(_currentIndex + 1) / _animals.Count : 0;
            using (var br = new SolidBrush(C_GOLD))
                g.FillRectangle(br, 20, barY, (int)(barW * pct), 3);
        }

        // ════════════════════════════════════════════════════════════
        //  LOAD SPOTLIGHT
        // ════════════════════════════════════════════════════════════
        private void LoadSpotlight(int idx)
        {
            var a = _animals[idx];

            // Load photo from embedded Resources
            try
            {
                _currentPhoto?.Dispose();
                _currentPhoto = LoadPhoto(a.PhotoFile);
            }
            catch { _currentPhoto = null; }

            picAnimalPhoto.Invalidate();
            pnlFactBox.Invalidate();
            pnlQueue.Invalidate();

            lblAnimalCounter.Text = $"ANIMAL {idx + 1} OF {_animals.Count}";
            lblConservationStatus.Text = a.Conservation.ToUpper();
            lblConservationStatus.ForeColor = a.ConsColor;
            // Place conservation status top-right of picAnimalPhoto
            lblConservationStatus.Location = new Point(picAnimalPhoto.Width - lblConservationStatus.PreferredWidth - 48, 28);

            lblZoneBadge.Text = $"● {a.Zone.ToUpper()}";
            lblZoneBadge.Location = new Point(48, 50);

            lblAnimalName.Text = a.Name;
            lblAnimalName.Location = new Point(48, 82);

            lblSpecies.Text = a.Species;
            lblSpecies.Location = new Point(48, lblAnimalName.Bottom + 6);

            BuildStatPills(a);

            lblDescription.Text = a.Desc;
            lblDescription.Location = new Point(48, flpStatPills.Bottom + 18);

            pnlFactBox.Location = new Point(48, lblDescription.Bottom + 12);
            flpTags.Location = new Point(48, pnlFactBox.Bottom + 14);

            BuildTags(a);

            // "Up Next" strip
            int nextIdx = (idx + 1) % _animals.Count;
            var next = _animals[nextIdx];
            lblUpNext.Location = new Point(100, 20);
            lblNextAnimalName.Text = next.Name;
            lblNextAnimalName.Location = new Point(100, 38);

            try { picNextAnimal.Image = LoadPhoto(next.PhotoFile); }
            catch { }

            BuildQueue(idx);
            lblQueueCount.Text = $"{idx + 1} / {_animals.Count}";

            // Position spotlight button now that layout is done
            PositionSpotlightBtn();
        }

        private void BuildStatPills(AnimalData a)
        {
            flpStatPills.Controls.Clear();
            string[,] pills = { { a.StatA, "KEY STAT" }, { a.StatB, "STATUS" }, { a.StatC, "ZONE" } };

            for (int i = 0; i < 3; i++)
            {
                var pnl = new Panel
                {
                    Size = new Size(175, 58),
                    BackColor = Color.Transparent,
                    Margin = new Padding(0, 0, 10, 0),
                    Cursor = Cursors.Default
                };
                pnl.Paint += (s, pe) =>
                {
                    var g = pe.Graphics;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    var pr = pnl.ClientRectangle;
                    pr.Inflate(-1, -1);
                    using (var path = MakeRoundedPath(pr, 10))
                    {
                        using (var fill = new SolidBrush(Color.FromArgb(100, 0, 0, 0)))
                            g.FillPath(fill, path);
                        using (var border = new Pen(Color.FromArgb(55, 255, 255, 255), 1))
                            g.DrawPath(border, path);
                    }
                };

                string val = pills[i, 0], lbl = pills[i, 1];
                pnl.Controls.Add(new Label { Text = val, Font = new Font("Georgia", 11f, FontStyle.Bold), ForeColor = C_CREAM, BackColor = Color.Transparent, AutoSize = false, Size = new Size(165, 28), Location = new Point(8, 8), TextAlign = ContentAlignment.MiddleLeft });
                pnl.Controls.Add(new Label { Text = lbl, Font = new Font("Segoe UI", 7f, FontStyle.Bold), ForeColor = Color.FromArgb(100, 248, 244, 239), BackColor = Color.Transparent, AutoSize = false, Size = new Size(165, 16), Location = new Point(8, 36), TextAlign = ContentAlignment.MiddleLeft });
                flpStatPills.Controls.Add(pnl);
            }

            flpStatPills.Location = new Point(48, lblSpecies.Bottom + 32);
            flpStatPills.Size = new Size(555, 62);
        }

        private void BuildTags(AnimalData a)
        {
            flpTags.Controls.Clear();
            foreach (var tag in a.Tags)
            {
                var lbl = new Label
                {
                    Text = tag,
                    Font = new Font("Segoe UI", 9f),
                    ForeColor = Color.FromArgb(200, 248, 244, 239),
                    BackColor = Color.Transparent,
                    AutoSize = true,
                    Padding = new Padding(10, 4, 10, 4),
                    Margin = new Padding(0, 0, 8, 0),
                    Cursor = Cursors.Default
                };
                lbl.Paint += (s, pe) =>
                {
                    pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    var lr = lbl.ClientRectangle;
                    lr.Inflate(-1, -1);
                    using (var path = MakeRoundedPath(lr, lr.Height / 2))
                    {
                        using (var fill = new SolidBrush(Color.FromArgb(80, 0, 0, 0)))
                            pe.Graphics.FillPath(fill, path);
                        using (var pen = new Pen(Color.FromArgb(60, 255, 255, 255), 1))
                            pe.Graphics.DrawPath(pen, path);
                    }
                };
                flpTags.Controls.Add(lbl);
            }
        }

        private void BuildQueue(int activeIdx)
        {
            flpQueueList.Controls.Clear();
            int show = Math.Min(5, _animals.Count);

            for (int i = 0; i < show; i++)
            {
                int ri = (activeIdx + i) % _animals.Count;
                var a = _animals[ri];
                bool isActive = i == 0;
                bool isDim = i >= 3;

                if (i > 0)
                    flpQueueList.Controls.Add(new Panel
                    {
                        Size = new Size(360, 1),
                        BackColor = Color.FromArgb(22, 212, 160, 23),
                        Margin = new Padding(10, 0, 10, 0)
                    });

                var item = new Panel
                {
                    Size = new Size(380, 70),
                    BackColor = isActive ? Color.FromArgb(35, 212, 160, 23) : Color.Transparent,
                    Cursor = Cursors.Default,
                    Margin = new Padding(0)
                };

                item.Paint += (s, pe) =>
                {
                    if (isActive)
                    {
                        pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                        using (var pen = new Pen(Color.FromArgb(80, 212, 160, 23), 1))
                            pe.Graphics.DrawRectangle(pen, 0, 0, item.Width - 1, item.Height - 1);
                    }
                };

                var pic = new PictureBox
                {
                    Size = new Size(52, 52),
                    Location = new Point(14, 9),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BackColor = Color.FromArgb(20, 40, 24)
                };
                try { pic.Image = LoadPhoto(a.PhotoFile); } catch { }

                item.Controls.Add(pic);
                item.Controls.Add(new Label { Text = isActive ? "▶" : i.ToString(), Font = new Font("Segoe UI", 8f, FontStyle.Bold), ForeColor = isActive ? C_GOLD : Color.FromArgb(60, 212, 160, 23), BackColor = Color.Transparent, AutoSize = true, Location = new Point(74, 27) });
                item.Controls.Add(new Label { Text = a.Name, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), ForeColor = isDim ? Color.FromArgb(80, 248, 244, 239) : C_CREAM, BackColor = Color.Transparent, AutoSize = false, Size = new Size(220, 22), Location = new Point(92, 12) });
                item.Controls.Add(new Label { Text = a.Species, Font = new Font("Segoe UI", 8f, FontStyle.Italic), ForeColor = isDim ? Color.FromArgb(50, 248, 244, 239) : Color.FromArgb(80, 248, 244, 239), BackColor = Color.Transparent, AutoSize = false, Size = new Size(220, 16), Location = new Point(92, 34) });
                item.Controls.Add(new Label { Text = $"● {a.Zone}", Font = new Font("Segoe UI", 7.5f, FontStyle.Bold), ForeColor = isDim ? Color.FromArgb(40, 212, 160, 23) : Color.FromArgb(150, 212, 160, 23), BackColor = Color.Transparent, AutoSize = false, Size = new Size(220, 14), Location = new Point(92, 50) });

                if (isActive)
                    item.Controls.Add(new Label
                    {
                        Text = "NOW",
                        Font = new Font("Segoe UI", 7f, FontStyle.Bold),
                        ForeColor = Color.FromArgb(7, 26, 14),
                        BackColor = C_GOLD,
                        AutoSize = false,
                        Size = new Size(36, 18),
                        Location = new Point(330, 26),
                        TextAlign = ContentAlignment.MiddleCenter
                    });

                flpQueueList.Controls.Add(item);
            }

            int remaining = _animals.Count - show;
            flpQueueList.Controls.Add(new Label
            {
                Text = $"↓  +{remaining} more cycling in the loop",
                Font = new Font("Segoe UI", 8f),
                ForeColor = Color.FromArgb(50, 248, 244, 239),
                BackColor = Color.Transparent,
                AutoSize = false,
                Size = new Size(380, 30),
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(0, 6, 0, 0)
            });
        }

        // ════════════════════════════════════════════════════════════
        //  ADVANCE ANIMAL
        // ════════════════════════════════════════════════════════════
        private void AdvanceAnimal()
        {
            _currentIndex = (_currentIndex + 1) % _animals.Count;
            LoadSpotlight(_currentIndex);
        }

        // ════════════════════════════════════════════════════════════
        //  VIEW SWITCHER
        // ════════════════════════════════════════════════════════════
        private void SwitchView(bool spotlight)
        {
            _spotlightActive = spotlight;

            btnSpotlight.Tag = spotlight;
            btnAllResidents.Tag = !spotlight;
            btnSpotlight.Invalidate();
            btnAllResidents.Invalidate();

            if (spotlight)
            {
                pnlPhotoArea.Visible = true;
                pnlQueue.Visible = true;

                if (_ucAllResidents != null && pnlContentArea.Controls.Contains(_ucAllResidents))
                    pnlContentArea.Controls.Remove(_ucAllResidents);

                // Re-position the spotlight button after the panel becomes visible
                BeginInvoke(new Action(() => PositionSpotlightBtn()));
            }
            else
            {
                pnlPhotoArea.Visible = false;
                pnlQueue.Visible = false;

                if (_ucAllResidents == null)
                {
                    _ucAllResidents = new UcAllResidents(_animals.ConvertAll(a => new UcAllResidents.AnimalInfo
                    {
                        Name = a.Name,
                        Species = a.Species,
                        Zone = a.Zone,
                        Category = a.Category,
                        Conservation = a.Conservation,
                        ConsColor = a.ConsColor,
                        PhotoFile = a.PhotoFile,
                        Emoji = a.Emoji
                    }));
                    _ucAllResidents.Dock = DockStyle.Fill;
                }

                if (!pnlContentArea.Controls.Contains(_ucAllResidents))
                    pnlContentArea.Controls.Add(_ucAllResidents);

                _ucAllResidents.BringToFront();
            }
        }

        // ════════════════════════════════════════════════════════════
        //  HELPER: rounded GraphicsPath
        // ════════════════════════════════════════════════════════════
        private static GraphicsPath MakeRoundedPath(Rectangle r, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
