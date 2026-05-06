const path = require("path");
const fs = require("fs");
const bundledNodeModules = path.resolve(
  "C:\\Users\\JAMES\\.cache\\codex-runtimes\\codex-primary-runtime\\dependencies\\node\\node_modules"
);
const PptxGenJS = require(path.join(bundledNodeModules, "pptxgenjs"));

const root = path.resolve(__dirname, "..");
const outDir = __dirname;
const outFile = path.join(outDir, "WildNest_Final_Project_Presentation.pptx");

const pptx = new PptxGenJS();
pptx.layout = "LAYOUT_WIDE";
pptx.author = "OpenAI Codex";
pptx.company = "WildNest";
pptx.subject = "WildNest final project presentation";
pptx.title = "WildNest Zoo Resort and Wildlife Experience";
pptx.lang = "en-PH";
pptx.theme = {
  headFontFace: "Georgia",
  bodyFontFace: "Aptos",
  lang: "en-PH",
};

const C = {
  forest: "062A16",
  forest2: "0C3A22",
  forest3: "163F2A",
  gold: "D7A315",
  gold2: "F0C84C",
  cream: "F8F1E8",
  ink: "162117",
  mist: "EAE4DA",
  line: "36543F",
  soft: "AFC5B0",
  success: "8ECF9A",
};

function asset(...parts) {
  return path.join(root, ...parts);
}

function safeAsset(...parts) {
  const p = asset(...parts);
  return fs.existsSync(p) ? p : null;
}

function addBg(slide) {
  slide.background = { color: C.forest };
  slide.addShape(pptx.ShapeType.rect, {
    x: 0,
    y: 0,
    w: 13.333,
    h: 0.4,
    fill: { color: C.forest2 },
    line: { color: C.forest2 },
  });
  slide.addShape(pptx.ShapeType.line, {
    x: 0,
    y: 0.4,
    w: 13.333,
    h: 0,
    line: { color: C.gold, pt: 1.2 },
  });
  slide.addShape(pptx.ShapeType.line, {
    x: 0.45,
    y: 0.85,
    w: 12.4,
    h: 0,
    line: { color: C.line, pt: 1 },
  });
}

function addFooter(slide, left, center, right) {
  slide.addShape(pptx.ShapeType.rect, {
    x: 0,
    y: 7.08,
    w: 13.333,
    h: 0.42,
    fill: { color: C.forest2 },
    line: { color: C.forest2 },
  });
  slide.addText(left, {
    x: 0.55,
    y: 7.16,
    w: 3.6,
    h: 0.2,
    fontFace: "Aptos",
    fontSize: 10,
    color: C.gold2,
  });
  slide.addText(center, {
    x: 4.35,
    y: 7.16,
    w: 4.6,
    h: 0.2,
    fontFace: "Aptos",
    fontSize: 10,
    color: C.gold2,
    align: "center",
  });
  slide.addText(right, {
    x: 9.2,
    y: 7.16,
    w: 3.5,
    h: 0.2,
    fontFace: "Aptos",
    fontSize: 10,
    color: C.gold2,
    align: "right",
  });
}

function addBrand(slide) {
  const logo = safeAsset("Resources", "Logo.png");
  if (logo) {
    slide.addImage({ path: logo, x: 0.34, y: 0.14, w: 0.45, h: 0.45 });
  }
  slide.addText("WILDNEST", {
    x: 0.85,
    y: 0.13,
    w: 2.1,
    h: 0.28,
    fontFace: "Georgia",
    bold: true,
    fontSize: 24,
    color: C.cream,
  });
}

function addSectionTitle(slide, kicker, title, subtitle) {
  slide.addText(kicker.toUpperCase(), {
    x: 0.72,
    y: 0.65,
    w: 1.9,
    h: 0.22,
    fontFace: "Aptos",
    bold: true,
    fontSize: 10,
    color: C.gold2,
    charSpace: 0.6,
  });
  slide.addText(title, {
    x: 0.7,
    y: 0.92,
    w: 7.8,
    h: 0.56,
    fontFace: "Georgia",
    bold: true,
    fontSize: 24,
    color: C.cream,
  });
  if (subtitle) {
    slide.addText(subtitle, {
      x: 0.72,
      y: 1.44,
      w: 8.4,
      h: 0.5,
      fontFace: "Aptos",
      fontSize: 12,
      color: C.mist,
      breakLine: false,
    });
  }
}

function addCard(slide, x, y, w, h, opts = {}) {
  slide.addShape(pptx.ShapeType.roundRect, {
    x,
    y,
    w,
    h,
    rectRadius: 0.08,
    fill: { color: opts.fill || "0E301C", transparency: opts.transparency ?? 0 },
    line: { color: opts.line || C.gold, pt: opts.linePt || 0.8, transparency: opts.lineTransparency ?? 0 },
    shadow: opts.shadow
      ? { type: "outer", color: "000000", blur: 2, angle: 45, distance: 1, opacity: 0.18 }
      : undefined,
  });
}

function addBulletList(slide, items, x, y, w, h, fontSize = 14, color = C.cream) {
  const runs = [];
  items.forEach((text) => {
    runs.push({
      text,
      options: {
        bullet: { indent: 14 },
        hanging: 2,
        breakLine: true,
      },
    });
  });
  slide.addText(runs, {
    x,
    y,
    w,
    h,
    fontFace: "Aptos",
    fontSize,
    color,
    paraSpaceAfterPt: 8,
    margin: 0.02,
    valign: "top",
  });
}

function addMetric(slide, x, y, w, h, value, label, fill = "173B27") {
  addCard(slide, x, y, w, h, { fill, line: C.line, linePt: 0.8 });
  slide.addText(value, {
    x: x + 0.16,
    y: y + 0.12,
    w: w - 0.32,
    h: 0.3,
    fontFace: "Georgia",
    bold: true,
    fontSize: 22,
    color: C.gold2,
    align: "center",
  });
  slide.addText(label, {
    x: x + 0.14,
    y: y + 0.48,
    w: w - 0.28,
    h: 0.2,
    fontFace: "Aptos",
    fontSize: 10,
    color: C.mist,
    align: "center",
  });
}

function addImageSafe(slide, p, x, y, w, h) {
  if (p && fs.existsSync(p)) slide.addImage({ path: p, x, y, w, h });
}

// Slide 1: Title
{
  const slide = pptx.addSlide();
  addBg(slide);
  addBrand(slide);
  slide.addShape(pptx.ShapeType.rect, {
    x: 0,
    y: 0.4,
    w: 13.333,
    h: 6.68,
    fill: { color: C.forest3 },
    line: { color: C.forest3 },
  });
  slide.addShape(pptx.ShapeType.rect, {
    x: 0,
    y: 0.4,
    w: 13.333,
    h: 6.68,
    fill: { color: "FFFFFF", transparency: 93 },
    line: { color: "FFFFFF", transparency: 100 },
  });
  slide.addText("WILDNEST", {
    x: 0.9,
    y: 1.05,
    w: 2.4,
    h: 0.3,
    fontFace: "Aptos",
    bold: true,
    fontSize: 11,
    color: C.gold2,
    charSpace: 1.3,
  });
  slide.addText("Zoo Resort and Wildlife Experience", {
    x: 0.88,
    y: 1.45,
    w: 7.6,
    h: 0.72,
    fontFace: "Georgia",
    bold: true,
    fontSize: 28,
    color: C.cream,
  });
  slide.addText("Final Project Presentation", {
    x: 0.9,
    y: 2.3,
    w: 3.8,
    h: 0.32,
    fontFace: "Aptos",
    bold: true,
    fontSize: 15,
    color: C.gold2,
  });
  slide.addText(
    "A C# WinForms and MySQL desktop platform for premium bookings, guest access, wildlife operations, reception check-in, analytics, and internal staff coordination.",
    {
      x: 0.9,
      y: 2.78,
      w: 6.15,
      h: 1.2,
      fontFace: "Aptos",
      fontSize: 16,
      color: C.mist,
      margin: 0.02,
    }
  );
  addMetric(slide, 0.95, 4.3, 1.7, 0.95, "4", "Booking Flows");
  addMetric(slide, 2.85, 4.3, 1.7, 0.95, "5", "Core Portals");
  addMetric(slide, 4.75, 4.3, 1.7, 0.95, "35", "Animal Residents");
  addMetric(slide, 6.65, 4.3, 1.7, 0.95, "1", "Unified Resort App");
  addImageSafe(slide, safeAsset("Resources", "Logo.png"), 9.25, 1.2, 2.5, 2.5);
  addImageSafe(slide, safeAsset("Resources", "Map.png"), 8.8, 3.7, 3.6, 2.45);
  addFooter(slide, "C# WinForms", "MySQL / WebView2 / QR / Mail", "WildNest Final Defense");
}

// Slide 2: Introduction
{
  const slide = pptx.addSlide();
  addBg(slide);
  addBrand(slide);
  addSectionTitle(
    slide,
    "Introduction",
    "What is WildNest?",
    "WildNest is a full desktop resort-management platform designed to connect bookings, wildlife operations, staff coordination, and guest services in one premium application."
  );
  addCard(slide, 0.72, 2.05, 6.1, 2.1, { fill: "103220", line: C.line });
  slide.addText("Core Problem", {
    x: 0.95,
    y: 2.28,
    w: 1.6,
    h: 0.25,
    fontFace: "Aptos",
    fontSize: 12,
    bold: true,
    color: C.gold2,
  });
  addBulletList(
    slide,
    [
      "Traditional resort and zoo workflows were scattered across paper records, manual coordination, and disconnected guest communication.",
      "Bookings, check-ins, animal information, staff coordination, analytics, and follow-up reporting needed one shared operational system.",
      "WildNest solves this through a role-based C# WinForms platform backed by MySQL and enhanced with QR, WebView, and email automation."
    ],
    0.95,
    2.58,
    5.45,
    1.2,
    14
  );
  addCard(slide, 7.05, 2.05, 5.55, 2.1, { fill: "103220", line: C.line });
  slide.addText("Real Users in the System", {
    x: 7.28,
    y: 2.28,
    w: 2.3,
    h: 0.25,
    fontFace: "Aptos",
    fontSize: 12,
    bold: true,
    color: C.gold2,
  });
  addBulletList(
    slide,
    [
      "Guests: browse, book, scan QR, and access portal records.",
      "Manager: oversees accounts, reports, billing, analytics, and operations.",
      "Reception, Tour Guides, and Zoo Keepers: execute day-to-day workflows and internal coordination."
    ],
    7.28,
    2.58,
    4.9,
    1.2,
    14
  );
  addMetric(slide, 0.9, 4.65, 2.2, 0.9, "Guest", "Public Booking + Portal");
  addMetric(slide, 3.25, 4.65, 2.2, 0.9, "Reception", "Check-In + Chat");
  addMetric(slide, 5.6, 4.65, 2.2, 0.9, "Zoo Keeper", "Animal Operations");
  addMetric(slide, 7.95, 4.65, 2.2, 0.9, "Tour Guide", "Tours + Messaging");
  addMetric(slide, 10.3, 4.65, 2.2, 0.9, "Manager", "Reports + Accounts");
  addImageSafe(slide, safeAsset("Resources", "Animals", "AfricanLion.jpg"), 8.3, 5.75, 1.6, 1.05);
  addImageSafe(slide, safeAsset("Resources", "House1.jpg"), 10.05, 5.75, 1.6, 1.05);
  addImageSafe(slide, safeAsset("Resources", "Map.png"), 1.05, 5.72, 1.55, 1.08);
  addFooter(slide, "Problem + Context", "From scattered workflows to one connected platform", "Slide 2");
}

// Slide 3: Objectives
{
  const slide = pptx.addSlide();
  addBg(slide);
  addBrand(slide);
  addSectionTitle(
    slide,
    "Objectives",
    "Project Objectives",
    "The system was designed not only as a booking app, but as a complete operational environment for a premium zoo resort."
  );
  const objectives = [
    ["01", "Centralize bookings", "Support cabin stays, day visits, experiences, and full-stay packages in one flow."],
    ["02", "Improve guest experience", "Provide QR-enabled confirmation, email proof, and a guest portal for access and follow-up."],
    ["03", "Modernize resort operations", "Give reception, tour guides, zoo keepers, and management dedicated role-based tools."],
    ["04", "Strengthen coordination", "Support guest chat, staff-to-staff direct messaging, and operational handoff visibility."],
    ["05", "Track wildlife and sanctuary content", "Present animal residents, zones, and attraction data inside a branded application."],
    ["06", "Generate decisions from data", "Deliver billing, revenue, search, reporting, and role-managed analytics to the manager."],
  ];
  let x = 0.72, y = 2.0;
  objectives.forEach((obj, i) => {
    addCard(slide, x, y, 4.05, 1.1, { fill: i % 2 === 0 ? "0E301C" : "153B28", line: C.line, shadow: true });
    slide.addText(obj[0], {
      x: x + 0.18, y: y + 0.14, w: 0.4, h: 0.24,
      fontFace: "Georgia", bold: true, fontSize: 20, color: C.gold2, align: "center"
    });
    slide.addText(obj[1], {
      x: x + 0.68, y: y + 0.14, w: 2.8, h: 0.2,
      fontFace: "Aptos", bold: true, fontSize: 13, color: C.cream
    });
    slide.addText(obj[2], {
      x: x + 0.68, y: y + 0.42, w: 3.05, h: 0.46,
      fontFace: "Aptos", fontSize: 10.5, color: C.mist, margin: 0.01
    });
    if (x > 4.8) { x = 0.72; y += 1.25; } else { x += 4.3; }
  });
  addFooter(slide, "Clear project goals", "Built for both guests and resort operations", "Slide 3");
}

// Slide 4: Modules
{
  const slide = pptx.addSlide();
  addBg(slide);
  addBrand(slide);
  addSectionTitle(
    slide,
    "Modules",
    "System Features and Major Modules",
    "WildNest combines public, guest, staff, wildlife, and analytics features inside one shared codebase."
  );
  const modules = [
    ["Public Homepage", "Cabins, experiences, map, animals, about, visit, and entry points to booking."],
    ["Book Now Engine", "Four booking flows with shared persistence, validation, payment choice, ID generation, and confirmation email."],
    ["Guest Portal", "Booking lookup, QR access, premium/limited experience, downloadable proof, and hosted guest pass path."],
    ["Reception Tools", "New booking, check-in, QR/camera flow, guest chat, billing, cabin availability, and encounter booking."],
    ["Manager Tools", "Staff accounts, reports, analytics, billing, search, dashboard metrics, and sales export."],
    ["Wildlife Content", "Animal records, sanctuary map, residents showcase, and zoo-operations context for the resort identity."],
  ];
  let y = 2.05;
  modules.forEach((m, idx) => {
    addCard(slide, 0.85 + (idx % 2) * 6.0, y, 5.6, 0.95, { fill: idx % 2 === 0 ? "103220" : "173A28", line: C.line });
    slide.addText(`${idx + 1}. ${m[0]}`, {
      x: 1.05 + (idx % 2) * 6.0,
      y: y + 0.14,
      w: 2.4,
      h: 0.2,
      fontFace: "Aptos",
      bold: true,
      fontSize: 13,
      color: C.gold2,
    });
    slide.addText(m[1], {
      x: 1.05 + (idx % 2) * 6.0,
      y: y + 0.38,
      w: 5.05,
      h: 0.35,
      fontFace: "Aptos",
      fontSize: 11.2,
      color: C.mist,
      margin: 0.01,
    });
    if (idx % 2 === 1) y += 1.12;
  });
  addFooter(slide, "Feature coverage", "One codebase, multiple operational surfaces", "Slide 4");
}

// Slide 5: Booking flow
{
  const slide = pptx.addSlide();
  addBg(slide);
  addBrand(slide);
  addSectionTitle(
    slide,
    "Booking Flow",
    "Four Booking Flows, One Shared Engine",
    "WildNest handles multiple reservation models while still writing into a unified backend flow."
  );
  const lanes = [
    ["Cabin Stay", "Check-in/check-out, guests, cabin selection, add-ons, payment choice"],
    ["Day Visit", "Visit date, guest count, selected activities, payment profile"],
    ["Experience Visit", "Guided encounter choice, visit schedule, guest details, payment"],
    ["Full Stay + Experience", "Combined stay + experience package with premium summary and confirmation"],
  ];
  let y = 2.15;
  lanes.forEach((lane, i) => {
    addCard(slide, 0.85, y, 5.5, 0.82, { fill: i % 2 === 0 ? "0F311F" : "153B28", line: C.line });
    slide.addText(lane[0], {
      x: 1.05, y: y + 0.14, w: 1.7, h: 0.2,
      fontFace: "Aptos", bold: true, fontSize: 13, color: C.gold2
    });
    slide.addText(lane[1], {
      x: 2.05, y: y + 0.14, w: 4.0, h: 0.36,
      fontFace: "Aptos", fontSize: 10.5, color: C.mist
    });
    y += 0.96;
  });
  const flowX = 7.0;
  const flow = [
    ["Homepage / Booking Entry", "Guest chooses a booking type"],
    ["Validation + Security", "Date rules, totals, and safe input handling"],
    ["MySQL Write", "Reservation, guest, payment, and booking-experience records"],
    ["ID + QR + Email", "Booking ID generation, QR pass, and confirmation email"],
    ["Guest Access", "Guest portal, hosted pass, and future reception scan path"],
  ];
  let fy = 2.05;
  flow.forEach((step, i) => {
    addCard(slide, flowX, fy, 5.15, 0.7, { fill: "113521", line: C.gold, linePt: 0.9 });
    slide.addText(`${i + 1}`, {
      x: flowX + 0.14, y: fy + 0.11, w: 0.3, h: 0.18,
      fontFace: "Georgia", bold: true, fontSize: 18, color: C.gold2, align: "center"
    });
    slide.addText(step[0], {
      x: flowX + 0.52, y: fy + 0.09, w: 1.8, h: 0.18,
      fontFace: "Aptos", bold: true, fontSize: 11.5, color: C.cream
    });
    slide.addText(step[1], {
      x: flowX + 2.25, y: fy + 0.09, w: 2.5, h: 0.22,
      fontFace: "Aptos", fontSize: 10, color: C.mist
    });
    if (i < flow.length - 1) {
      slide.addShape(pptx.ShapeType.chevron, {
        x: flowX + 2.37, y: fy + 0.72, w: 0.4, h: 0.2,
        fill: { color: C.gold }, line: { color: C.gold }
      });
    }
    fy += 0.95;
  });
  addFooter(slide, "Booking engine", "Shared architecture behind four reservation types", "Slide 5");
}

// Slide 6: QR and portal
{
  const slide = pptx.addSlide();
  addBg(slide);
  addBrand(slide);
  addSectionTitle(
    slide,
    "QR and Guest Access",
    "Email Confirmation, QR Pass, and Guest Portal",
    "The booking proof is not only emailed to the guest; it also becomes an access key into later guest and staff workflows."
  );
  addCard(slide, 0.82, 2.0, 4.05, 3.5, { fill: "103220", line: C.line, shadow: true });
  slide.addText("Guest receives", {
    x: 1.04, y: 2.2, w: 1.4, h: 0.2,
    fontFace: "Aptos", bold: true, fontSize: 12.5, color: C.gold2
  });
  addBulletList(slide, [
    "Booking ID",
    "QR code / hosted pass link",
    "Reservation summary",
    "Payment method and status",
    "Downloadable proof guidance"
  ], 1.05, 2.5, 3.25, 1.45, 13);
  addImageSafe(slide, safeAsset("Resources", "Logo.png"), 1.35, 4.1, 1.2, 1.2);
  slide.addText("Supported access paths", {
    x: 5.2, y: 2.06, w: 2.0, h: 0.2, fontFace: "Aptos", bold: true, fontSize: 12.5, color: C.gold2
  });
  const access = [
    "Booking ID + email lookup in the guest module",
    "QR-assisted guest portal entry",
    "Hosted public guest pass flow for mobile access",
    "Reception scan / manual fallback during check-in"
  ];
  addCard(slide, 5.0, 2.0, 7.45, 3.5, { fill: "103220", line: C.line });
  addBulletList(slide, access, 5.25, 2.42, 6.8, 1.55, 14);
  addCard(slide, 5.25, 4.1, 2.1, 1.0, { fill: "173B27", line: C.line });
  addCard(slide, 7.65, 4.1, 2.1, 1.0, { fill: "173B27", line: C.line });
  addCard(slide, 10.05, 4.1, 2.1, 1.0, { fill: "173B27", line: C.line });
  slide.addText("QRCoder", { x: 5.6, y: 4.32, w: 1.4, h: 0.18, color: C.cream, fontSize: 14, bold: true });
  slide.addText("WebView2", { x: 8.02, y: 4.32, w: 1.4, h: 0.18, color: C.cream, fontSize: 14, bold: true });
  slide.addText("MailKit", { x: 10.43, y: 4.32, w: 1.4, h: 0.18, color: C.cream, fontSize: 14, bold: true });
  addFooter(slide, "Guest access continuity", "Booking proof becomes part of later operations", "Slide 6");
}

// Slide 7: Staff portals
{
  const slide = pptx.addSlide();
  addBg(slide);
  addBrand(slide);
  addSectionTitle(
    slide,
    "Staff Portals",
    "Role-Based Operational Design",
    "Different staff groups use different portal surfaces, but they operate on the same shared system and database."
  );
  const roles = [
    ["Manager", "Owns analytics, staff accounts, billing, reports, and executive visibility."],
    ["Reception", "Handles bookings, guest profiles, check-in, billing, and guest chat."],
    ["Tour Guide", "Monitors guided encounter assignments, schedules, and internal staff coordination."],
    ["Zoo Keeper", "Maintains wildlife-related operations, records, and coordination context."],
  ];
  let x = 0.8;
  roles.forEach((r) => {
    addCard(slide, x, 2.1, 2.95, 2.15, { fill: "103220", line: C.gold, linePt: 0.9, shadow: true });
    slide.addText(r[0], {
      x: x + 0.18, y: 2.34, w: 2.3, h: 0.24,
      fontFace: "Georgia", bold: true, fontSize: 18, color: C.gold2, align: "center"
    });
    slide.addText(r[1], {
      x: x + 0.18, y: 2.76, w: 2.45, h: 1.05,
      fontFace: "Aptos", fontSize: 11, color: C.mist, align: "center", margin: 0.03
    });
    x += 3.1;
  });
  addCard(slide, 0.9, 4.75, 11.55, 1.25, { fill: "0E301C", line: C.line });
  slide.addText("Prompt 1 and Prompt 4 integration", {
    x: 1.15, y: 4.97, w: 3.2, h: 0.2,
    fontFace: "Aptos", bold: true, fontSize: 12.5, color: C.gold2
  });
  addBulletList(slide, [
    "Manager role was strengthened to become the operational authority replacing the old admin-only dependency.",
    "Internal staff chat evolved toward person-to-person coordination rather than only one shared role thread.",
    "Guest chat and internal staff chat remain separate operational surfaces."
  ], 1.15, 5.25, 10.7, 0.62, 12.5);
  addFooter(slide, "Role-based system", "Operational separation with shared backend continuity", "Slide 7");
}

// Slide 8: Manager analytics
{
  const slide = pptx.addSlide();
  addBg(slide);
  addBrand(slide);
  addSectionTitle(
    slide,
    "Manager Portal",
    "Analytics, Reports, and Staff Control",
    "The management layer focuses on executive visibility, account governance, and decision support."
  );
  addMetric(slide, 0.82, 2.0, 2.25, 0.95, "Sales Reports", "Daily / Weekly / Monthly / Yearly");
  addMetric(slide, 3.3, 2.0, 2.25, 0.95, "Billing", "Collection and payment review");
  addMetric(slide, 5.78, 2.0, 2.25, 0.95, "User Accounts", "Create, disable, archive, reset");
  addMetric(slide, 8.26, 2.0, 2.25, 0.95, "Search", "Guests, cabins, payments, chats");
  addMetric(slide, 10.74, 2.0, 1.78, 0.95, "Exports", "PDF / Word reports");
  addCard(slide, 0.84, 3.35, 5.95, 2.55, { fill: "103220", line: C.line });
  slide.addText("What the manager can do", {
    x: 1.06, y: 3.58, w: 2.4, h: 0.2, fontFace: "Aptos", bold: true, fontSize: 12.5, color: C.gold2
  });
  addBulletList(slide, [
    "Create new staff accounts for Reception, Tour Guides, and Zoo Keepers",
    "Toggle active / inactive state and safely archive users with operational history",
    "Review revenue, pending payment exposure, and booking activity by period",
    "Use reports to support business-owner style oversight through the manager role"
  ], 1.06, 3.95, 5.25, 1.55, 12.6);
  addCard(slide, 7.05, 3.35, 5.42, 2.55, { fill: "103220", line: C.line });
  slide.addText("Prompt 1 outcome", {
    x: 7.28, y: 3.58, w: 1.9, h: 0.2, fontFace: "Aptos", bold: true, fontSize: 12.5, color: C.gold2
  });
  addBulletList(slide, [
    "Administrator-heavy thinking was refocused into a stronger Manager portal.",
    "Business-owner reporting needs are supported while daily resort work remains staff-driven.",
    "This made the project closer to real resort workflow instead of a generic admin panel."
  ], 7.28, 3.95, 4.7, 1.35, 12.6);
  addFooter(slide, "Prompt 1", "Manager-centered reporting and governance", "Slide 8");
}

// Slide 9: Reception and check-in
{
  const slide = pptx.addSlide();
  addBg(slide);
  addBrand(slide);
  addSectionTitle(
    slide,
    "Reception Operations",
    "Check-In, Smart Verification, and Front Desk Flow",
    "Reception is one of the strongest live operational parts of WildNest because it connects bookings, guest proof, QR access, billing, and chat."
  );
  addCard(slide, 0.82, 2.0, 4.1, 3.55, { fill: "103220", line: C.line, shadow: true });
  slide.addText("Core front-desk features", {
    x: 1.04, y: 2.24, w: 2.2, h: 0.2,
    fontFace: "Aptos", bold: true, fontSize: 12.5, color: C.gold2
  });
  addBulletList(slide, [
    "New booking handoff using the shared Book Now flow",
    "Manual reservation lookup for check-in",
    "QR-assisted scanning path",
    "Guest profile visibility before confirmation",
    "Billing and payment follow-up surfaces"
  ], 1.05, 2.55, 3.35, 1.8, 12.4);
  addCard(slide, 5.2, 2.0, 7.2, 1.55, { fill: "103220", line: C.gold, linePt: 0.9 });
  slide.addText("Prompt 2 direction", {
    x: 5.45, y: 2.22, w: 1.9, h: 0.2, fontFace: "Aptos", bold: true, fontSize: 12.5, color: C.gold2
  });
  addBulletList(slide, [
    "The same booking proof that reaches the guest by email also supports later reception-side verification logic.",
    "This turns QR from a simple confirmation artifact into part of the resort’s operational check-in pipeline."
  ], 5.45, 2.54, 6.35, 0.72, 12.4);
  addCard(slide, 5.2, 3.8, 2.1, 1.55, { fill: "173B27", line: C.line });
  addCard(slide, 7.65, 3.8, 2.1, 1.55, { fill: "173B27", line: C.line });
  addCard(slide, 10.1, 3.8, 2.1, 1.55, { fill: "173B27", line: C.line });
  slide.addText("Lookup", { x: 5.96, y: 4.14, w: 0.6, h: 0.18, color: C.cream, fontSize: 14, bold: true, align: "center" });
  slide.addText("QR", { x: 8.46, y: 4.14, w: 0.5, h: 0.18, color: C.cream, fontSize: 14, bold: true, align: "center" });
  slide.addText("Confirm", { x: 10.74, y: 4.14, w: 0.8, h: 0.18, color: C.cream, fontSize: 14, bold: true, align: "center" });
  addFooter(slide, "Prompt 2", "Reception is where booking proof becomes operational action", "Slide 9");
}

// Slide 10: Guest portal
{
  const slide = pptx.addSlide();
  addBg(slide);
  addBrand(slide);
  addSectionTitle(
    slide,
    "Guest Portal",
    "Premium vs Limited Guest Experience",
    "WildNest does not stop at reservation creation; it extends the booking into a branded guest-facing follow-up environment."
  );
  addCard(slide, 0.8, 2.0, 5.75, 3.7, { fill: "103220", line: C.line, shadow: true });
  slide.addText("Limited access path", {
    x: 1.04, y: 2.22, w: 1.8, h: 0.2, fontFace: "Aptos", bold: true, fontSize: 12.5, color: C.gold2
  });
  addBulletList(slide, [
    "Basic booking verification",
    "Reservation proof visibility",
    "QR / booking essentials",
    "Minimal confirmation-oriented guest experience"
  ], 1.05, 2.56, 2.35, 1.0, 12.4);
  slide.addText("Premium portal direction", {
    x: 3.48, y: 2.22, w: 1.95, h: 0.2, fontFace: "Aptos", bold: true, fontSize: 12.5, color: C.gold2
  });
  addBulletList(slide, [
    "Richer dashboard view",
    "Downloadable materials",
    "Booking insights and visibility",
    "Branded hosted pass for off-network mobile access"
  ], 3.5, 2.56, 2.4, 1.0, 12.4);
  addCard(slide, 6.95, 2.0, 5.5, 3.7, { fill: "103220", line: C.line });
  slide.addText("Prompt 3 outcome", {
    x: 7.2, y: 2.22, w: 1.8, h: 0.2, fontFace: "Aptos", bold: true, fontSize: 12.5, color: C.gold2
  });
  addBulletList(slide, [
    "Guest access is no longer just a static reservation result.",
    "The QR/portal direction was pushed toward a premium pass experience accessible on another device.",
    "This gave the project a stronger hospitality and guest-retention identity."
  ], 7.22, 2.56, 4.65, 1.05, 12.4);
  addImageSafe(slide, safeAsset("Resources", "Logo.png"), 9.0, 4.05, 1.5, 1.5);
  addFooter(slide, "Prompt 3", "Guest experience continues after booking", "Slide 10");
}

// Slide 11: Architecture and database
{
  const slide = pptx.addSlide();
  addBg(slide);
  addBrand(slide);
  addSectionTitle(
    slide,
    "Architecture",
    "Codebase Structure, Database, and Packages",
    "WildNest combines Windows Forms presentation, shared business flow, MySQL storage, WebView-powered portal UI, and QR/email libraries."
  );
  addCard(slide, 0.82, 2.0, 4.55, 3.9, { fill: "103220", line: C.line });
  slide.addText("Technology stack", {
    x: 1.05, y: 2.22, w: 1.8, h: 0.2, fontFace: "Aptos", bold: true, fontSize: 12.5, color: C.gold2
  });
  addBulletList(slide, [
    ".NET 8 WinForms",
    "MySQL backend",
    "MailKit + MimeKit for email",
    "QRCoder and ZXing.Net for QR creation and scanning",
    "WebView2 for HTML-driven guest surfaces"
  ], 1.05, 2.55, 3.55, 1.6, 12.5);
  slide.addText("Important tables", {
    x: 1.05, y: 4.55, w: 1.8, h: 0.2, fontFace: "Aptos", bold: true, fontSize: 12.5, color: C.gold2
  });
  addBulletList(slide, [
    "tbl_reservations",
    "tbl_guests",
    "tbl_payments",
    "tbl_users",
    "tbl_bookingexperiences",
    "tbl_chat / tbl_staffmessages"
  ], 1.05, 4.85, 2.3, 0.9, 12.3);
  addCard(slide, 5.7, 2.0, 6.65, 3.9, { fill: "103220", line: C.line });
  slide.addText("Architecture view", {
    x: 5.95, y: 2.22, w: 1.8, h: 0.2, fontFace: "Aptos", bold: true, fontSize: 12.5, color: C.gold2
  });
  addImageSafe(slide, safeAsset("Resources", "ClassDiagram1.png"), 6.0, 2.55, 6.0, 2.85);
  addFooter(slide, "Technical foundation", "Desktop UI + database + QR + portal integration", "Slide 11");
}

// Slide 12: Conclusion
{
  const slide = pptx.addSlide();
  addBg(slide);
  addBrand(slide);
  slide.addText("Conclusion", {
    x: 0.9, y: 1.0, w: 2.2, h: 0.3,
    fontFace: "Aptos", bold: true, fontSize: 12, color: C.gold2, charSpace: 0.7
  });
  slide.addText("WildNest is a premium, code-complete\nresort operations platform", {
    x: 0.9, y: 1.45, w: 7.6, h: 1.05,
    fontFace: "Georgia", bold: true, fontSize: 26, color: C.cream
  });
  addBulletList(slide, [
    "Integrates public booking, guest follow-up, wildlife presentation, reception work, staff coordination, and manager reporting.",
    "Implements the major prompt directions: stronger Manager role, QR/check-in evolution, premium guest access, and richer staff chat.",
    "Demonstrates a realistic WinForms + MySQL architecture that can be extended into web, mobile, or real-time hosted services."
  ], 0.95, 2.8, 6.8, 1.7, 15);
  addCard(slide, 7.85, 1.65, 4.55, 3.15, { fill: "103220", line: C.gold, linePt: 1.0, shadow: true });
  slide.addText("Presentation highlights", {
    x: 8.18, y: 1.95, w: 2.2, h: 0.22,
    fontFace: "Aptos", bold: true, fontSize: 13, color: C.gold2
  });
  addBulletList(slide, [
    "Four booking flows",
    "Guest QR and hosted pass logic",
    "Role-based staff portals",
    "Manager analytics and reports",
    "Wildlife and sanctuary experience branding"
  ], 8.15, 2.32, 3.7, 1.7, 13.2);
  slide.addText("\"Where the wild meets comfort.\"", {
    x: 0.95, y: 5.35, w: 4.7, h: 0.3,
    fontFace: "Georgia", italic: true, fontSize: 22, color: C.gold2
  });
  addFooter(slide, "Final project defense", "WildNest Zoo Resort and Wildlife Experience", "Thank you");
}

fs.mkdirSync(outDir, { recursive: true });

(async () => {
  await pptx.writeFile({ fileName: outFile });
  console.log(outFile);
})().catch((err) => {
  console.error(err);
  process.exit(1);
});
