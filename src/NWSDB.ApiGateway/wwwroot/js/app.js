// NWSDB customer website - client application that consumes the NWSDB web services
// through the API gateway (same origin, so every call starts with /api/...).
"use strict";

const API_BASE = window.NWSDB_API_BASE || "";
const $ = id => document.getElementById(id);

// ---------------------------------------------------------------- session
let session = null; // { token, accountNo, fullName, role }
try { session = JSON.parse(sessionStorage.getItem("nwsdb-session")); } catch { session = null; }

function saveSession(s) {
  session = s;
  try { s ? sessionStorage.setItem("nwsdb-session", JSON.stringify(s)) : sessionStorage.removeItem("nwsdb-session"); } catch { /* storage blocked */ }
}

// ---------------------------------------------------------------- helpers
/** One reusable function for every API call: adds the JWT token and turns errors into friendly messages. */
async function api(path, { method = "GET", body } = {}) {
  const headers = { "Content-Type": "application/json" };
  if (session?.token) headers.Authorization = "Bearer " + session.token;

  let res;
  try {
    res = await fetch(API_BASE + path, { method, headers, body: body ? JSON.stringify(body) : undefined });
  } catch {
    throw new Error("Cannot reach the NWSDB services. Make sure the API gateway is running.");
  }

  const text = await res.text();
  let data = null;
  try { data = text ? JSON.parse(text) : null; } catch { data = null; }

  if (!res.ok) {
    if (data?.errors) throw new Error(Object.values(data.errors).flat().join(" "));
    if (data?.message) throw new Error(data.message);
    if (res.status === 401) throw new Error("Your session has expired. Please log in again.");
    if (res.status === 403) throw new Error("You do not have permission to do this.");
    if (res.status === 429) throw new Error("Too many requests. Please wait a minute and try again.");
    throw new Error("Something went wrong (error " + res.status + "). Please try again.");
  }
  return data;
}

function say(id, text, kind) {
  const el = $(id);
  el.textContent = text;
  el.className = "res" + (kind ? " " + kind : "");
}

const money = n => Number(n).toLocaleString("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const MONTHS = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
const monthLabel = ym => MONTHS[Number(ym.slice(5, 7)) - 1] + " " + ym.slice(0, 4);
const dateLabel = d => Number(d.slice(8, 10)) + " " + MONTHS[Number(d.slice(5, 7)) - 1] + " " + d.slice(0, 4);
const esc = s => String(s).replace(/[&<>"']/g, c => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]));
const pill = status => `<span class="pill ${esc(status)}">${esc(status)}</span>`;

async function busy(button, work) {
  button.disabled = true;
  try { await work(); } finally { button.disabled = false; }
}

// ---------------------------------------------------------------- quick bill check (no login needed)
$("q-go").onclick = () => busy($("q-go"), async () => {
  const acc = $("q-acc").value.trim();
  if (!acc) return say("q-res", "Enter your water account number.", "err");
  say("q-res", "Loading...");
  try {
    const bill = await api(`/api/bills/${encodeURIComponent(acc)}/current`);
    say("q-res", `Account ${bill.accountNo}: LKR ${money(bill.amount)} for ${monthLabel(bill.month)}, due on ${dateLabel(bill.dueDate)} (${bill.status}).`);
  } catch (e) { say("q-res", e.message, "err"); }
});

// ---------------------------------------------------------------- usage
async function loadUsage() {
  const acc = $("u-acc").value.trim();
  if (!session) return say("u-res", "Log in to see your water usage.", "err");
  if (!acc) return say("u-res", "Enter a water account number.", "err");
  say("u-res", "Loading...");
  try {
    const rows = await api(`/api/usage/${encodeURIComponent(acc)}?months=6`);
    const max = Math.max(...rows.map(r => r.unitsUsed), 1);
    $("chart").innerHTML = rows.map((r, i) =>
      `<div class="bar${i === rows.length - 1 ? " cur" : ""}" style="height:${r.unitsUsed / max * 88}%"><span>${r.unitsUsed}</span></div>`).join("");
    $("months").innerHTML = rows.map(r => `<i>${monthLabel(r.month).slice(0, 3)}</i>`).join("");
    const last = rows[rows.length - 1], prev = rows[rows.length - 2];
    let text = `Account ${acc}: ${last.unitsUsed} m³ used in ${monthLabel(last.month)}`;
    if (prev) {
      const diff = last.unitsUsed - prev.unitsUsed;
      text += diff === 0 ? ", the same as last month." : `, ${diff > 0 ? "up" : "down"} ${Math.abs(diff)} m³ from last month.`;
    }
    say("u-res", text + ` Meter reading: ${last.meterValue} m³.`);
  } catch (e) { say("u-res", e.message, "err"); }
}
$("u-go").onclick = () => busy($("u-go"), loadUsage);

// ---------------------------------------------------------------- billing (view and pay)
/** Shows the current bill and the bill history. View only: it never pays. */
async function loadBill() {
  const acc = $("b-acc").value.trim();
  if (!session) return;
  try {
    const [bill, history] = await Promise.all([
      api(`/api/bills/${encodeURIComponent(acc)}/current`),
      api(`/api/bills/${encodeURIComponent(acc)}/history`)
    ]);
    $("bill-box").hidden = false;
    $("bb-month").textContent = monthLabel(bill.month);
    $("bb-units").textContent = bill.unitsUsed + " m³";
    $("bb-amount").textContent = "LKR " + money(bill.amount);
    $("bb-due").textContent = dateLabel(bill.dueDate);
    $("bb-status").innerHTML = pill(bill.status);
    $("hist").innerHTML = history.map(h =>
      `<tr><td>${monthLabel(h.month)}</td><td>${h.unitsUsed}</td><td>${money(h.amount)}</td><td>${dateLabel(h.dueDate)}</td><td>${pill(h.status)}</td></tr>`).join("");
    if (bill.status === "Paid") say("b-res", "Your current bill is paid. Thank you.", "ok");
    else say("b-res", `Current bill: LKR ${money(bill.amount)} due on ${dateLabel(bill.dueDate)}. Choose a channel and select Pay current bill.`);
  } catch (e) { say("b-res", e.message, "err"); }
}

$("b-go").onclick = () => busy($("b-go"), async () => {
  if (!session) return say("b-res", "Log in to pay your bill.", "err");
  const acc = $("b-acc").value.trim();
  if (!acc) return say("b-res", "Enter a water account number.", "err");
  say("b-res", "Processing payment...");
  try {
    const receipt = await api("/api/payments", { method: "POST", body: { accountNo: acc, channel: $("b-ch").value } });
    await loadBill();
    say("b-res", `Paid LKR ${money(receipt.amount)} via ${receipt.channel}. Receipt: ${receipt.receiptNo}. An SMS receipt has been sent.`, "ok");
  } catch (e) { say("b-res", e.message, "err"); }
});

// ---------------------------------------------------------------- login / logout / register
$("l-go").onclick = () => busy($("l-go"), async () => {
  const user = $("l-u").value.trim(), pass = $("l-p").value;
  const missing = [];
  if (!user) missing.push("Account number is required.");
  if (!pass) missing.push("Password is required.");
  if (missing.length) return say("l-res", missing.join(" "), "err");

  say("l-res", "Signing in...");
  try {
    const result = await api("/api/auth/login", { method: "POST", body: { accountNo: user, password: pass } });
    saveSession(result);
    $("l-p").value = "";
    say("l-res", `Signed in as ${result.fullName}.` + (result.role === "Admin" ? " The Admin Dashboard is open below." : " Your bill, usage and history are loaded below."), "ok");
    await applySession();
    $(result.role === "Admin" ? "admin" : "usage").scrollIntoView();
  } catch (e) { say("l-res", e.message, "err"); }
});

$("l-out").onclick = () => {
  saveSession(null);
  applySession();
  say("l-res", "You have signed out.", "ok");
};

$("auth-btn").onclick = () => session ? $("l-out").click() : $("login").scrollIntoView();

$("r-go").onclick = () => busy($("r-go"), async () => {
  const f = {
    accountNo: $("r-acc").value.trim(), fullName: $("r-name").value.trim(), email: $("r-email").value.trim(),
    phone: $("r-phone").value.trim(), password: $("r-pass").value
  };
  const errors = [];
  if (!f.accountNo) errors.push("Account number is required.");
  if (!f.fullName) errors.push("Full name is required.");
  if (!f.email) errors.push("Email address is required.");
  else if (!$("r-email").checkValidity()) errors.push("Enter a valid email address.");
  if (!f.phone) errors.push("Phone number is required.");
  if (!f.password) errors.push("Password is required.");
  else if (f.password.length < 6) errors.push("Password must be at least 6 characters.");
  if (!$("r-pass2").value) errors.push("Please confirm your password.");
  else if ($("r-pass2").value !== f.password) errors.push("Passwords do not match.");
  if (errors.length) return say("r-res", errors.join(" "), "err");

  say("r-res", "Registering...");
  try {
    const r = await api("/api/auth/register", { method: "POST", body: f });
    say("r-res", r.message, "ok");
    $("l-u").value = f.accountNo;
    ["r-pass", "r-pass2"].forEach(id => $(id).value = "");
  } catch (e) { say("r-res", e.message, "err"); }
});

/** Updates the page for the signed-in user (or for a visitor). */
async function applySession() {
  const on = !!session, admin = session?.role === "Admin";
  $("auth-btn").textContent = on ? `Sign out (${session.accountNo})` : "Login";
  $("l-out").hidden = !on;
  $("l-go").hidden = on;
  $("l-u").disabled = $("l-p").disabled = on;
  document.querySelectorAll(".admin-only").forEach(el => el.hidden = !admin);

  ["u-acc", "b-acc"].forEach(id => {
    $(id).value = on && !admin ? session.accountNo : "";
    $(id).readOnly = on && !admin;
    $(id).placeholder = on ? "e.g. 1001" : "Log in first";
  });

  if (!on) {
    $("chart").innerHTML = $("months").innerHTML = "";
    $("bill-box").hidden = true;
    $("hist").innerHTML = `<tr><td colspan="5">Log in to see your bill history.</td></tr>`;
    say("u-res", "Log in to see your water usage.");
    say("b-res", "Log in to see and pay your bill.");
    return;
  }
  if (admin) {
    say("u-res", "Enter any account number to view its usage.");
    say("b-res", "Enter any account number, then pay on the customer's behalf.");
    await Promise.all([loadAllBills(), loadMessages()]);
  } else {
    await Promise.all([loadUsage(), loadBill()]);
  }
}

// ---------------------------------------------------------------- admin dashboard
let allBills = [];

async function loadAllBills() {
  try {
    allBills = await api("/api/bills");
    renderBills();
  } catch (e) { say("t-res", e.message, "err"); }
}

function renderBills() {
  const filter = $("f-acc").value.trim();
  const rows = allBills.filter(b => !filter || b.accountNo.includes(filter));
  $("all-bills").innerHTML = rows.length ? rows.map(b =>
    `<tr><td>${b.billId}</td><td>${esc(b.accountNo)}</td><td>${monthLabel(b.month)}</td><td>${b.unitsUsed}</td><td>${money(b.amount)}</td>` +
    `<td>${dateLabel(b.dueDate)}</td><td>${pill(b.status)}</td>` +
    `<td><button class="alt" data-edit="${b.billId}">Edit</button><button class="danger" data-del="${b.billId}">Delete</button></td></tr>`).join("")
    : `<tr><td colspan="8">No bills found.</td></tr>`;
}
$("f-acc").oninput = renderBills;

$("all-bills").onclick = async ev => {
  const editId = ev.target.dataset.edit, delId = ev.target.dataset.del;
  if (editId) {
    const b = allBills.find(x => x.billId === Number(editId));
    $("e-id").value = b.billId;
    $("e-amount").value = b.amount;
    $("e-due").value = b.dueDate;
    $("e-status").value = b.status;
    say("e-res", `Editing bill ${b.billId} for account ${b.accountNo} (${monthLabel(b.month)}).`);
    $("e-amount").focus();
  }
  if (delId) {
    if (!confirm(`Delete bill ${delId}? This cannot be undone.`)) return;
    try {
      await api(`/api/bills/${delId}`, { method: "DELETE" });
      await loadAllBills();
      say("t-res", `Bill ${delId} deleted.`, "ok");
    } catch (e) { say("t-res", e.message, "err"); }
  }
};

$("e-go").onclick = () => busy($("e-go"), async () => {
  const id = $("e-id").value;
  if (!id) return say("e-res", "Choose Edit next to a bill in the table first.", "err");
  if ($("e-amount").value === "" || !$("e-due").value) return say("e-res", "Amount and due date are required.", "err");
  try {
    const b = await api(`/api/bills/${id}`, {
      method: "PUT",
      body: { amount: Number($("e-amount").value), dueDate: $("e-due").value, status: $("e-status").value }
    });
    await loadAllBills();
    say("e-res", `Bill ${b.billId} updated: LKR ${money(b.amount)}, due ${dateLabel(b.dueDate)}, ${b.status}.`, "ok");
  } catch (e) { say("e-res", e.message, "err"); }
});

$("a-go").onclick = () => busy($("a-go"), async () => {
  const acc = $("a-acc").value.trim(), month = $("a-month").value, meter = $("a-meter").value,
        units = $("a-units").value, due = $("a-due").value;
  const errors = [];
  if (!acc) errors.push("Account number is required.");
  if (!month) errors.push("Month is required.");
  if (meter === "") errors.push("Meter value is required.");
  if (units === "") errors.push("Units used is required.");
  if (!due) errors.push("Due date is required.");
  if (errors.length) return say("a-res", errors.join(" "), "err");

  try {
    await api("/api/usage", { method: "POST", body: { accountNo: acc, month, meterValue: Number(meter), unitsUsed: Number(units) } });
    const bill = await api("/api/bills", { method: "POST", body: { accountNo: acc, month, unitsUsed: Number(units), dueDate: due } });
    await loadAllBills();
    say("a-res", `Reading saved and bill created: LKR ${money(bill.amount)} for account ${bill.accountNo} (${monthLabel(bill.month)}).`, "ok");
  } catch (e) { say("a-res", e.message, "err"); }
});

async function loadMessages() {
  try {
    const list = await api("/api/contact");
    $("msgs").innerHTML = list.length ? list.map(m =>
      `<tr><td>${dateLabel(m.createdAt.slice(0, 10))}</td><td>${esc(m.name)}</td><td>${esc(m.email)}</td><td>${esc(m.message)}</td></tr>`).join("")
      : `<tr><td colspan="4">No messages yet.</td></tr>`;
  } catch { /* the table keeps its last value */ }
}

// ---------------------------------------------------------------- contact and customer services
$("c-go").onclick = () => busy($("c-go"), async () => {
  const body = { name: $("c-n").value.trim(), email: $("c-e").value.trim(), message: $("c-m").value.trim() };
  if (!body.name || !body.email || !body.message || !$("c-e").checkValidity())
    return say("c-res", "Enter your name, a valid email and a message.", "err");
  try {
    const r = await api("/api/contact", { method: "POST", body });
    say("c-res", r.message, "ok");
    $("c-m").value = "";
    if (session?.role === "Admin") loadMessages();
  } catch (e) { say("c-res", e.message, "err"); }
});

document.querySelectorAll("[data-topic]").forEach(btn => btn.onclick = () => {
  const acc = session && session.role !== "Admin" ? ` (account ${session.accountNo})` : "";
  $("c-m").value = btn.dataset.topic + acc + ": ";
  if (session) $("c-n").value = session.fullName;
  $("contact").scrollIntoView();
  $("c-m").focus();
});

// ---------------------------------------------------------------- theme
$("theme").onclick = () => {
  const root = document.documentElement;
  const dark = root.dataset.theme === "dark" || (!root.dataset.theme && matchMedia("(prefers-color-scheme: dark)").matches);
  root.dataset.theme = dark ? "light" : "dark";
};

// Enter key submits the nearest form button
[["q-acc", "q-go"], ["u-acc", "u-go"], ["l-p", "l-go"], ["l-u", "l-go"]].forEach(([input, button]) =>
  $(input).addEventListener("keydown", e => { if (e.key === "Enter") $(button).click(); }));

applySession();
