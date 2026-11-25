(()=>{var d={xmlns:"http://www.w3.org/2000/svg",width:24,height:24,viewBox:"0 0 24 24",fill:"none",stroke:"currentColor","stroke-width":2,"stroke-linecap":"round","stroke-linejoin":"round"};var G=([e,a,t])=>{let r=document.createElementNS("http://www.w3.org/2000/svg",e);return Object.keys(a).forEach(o=>{r.setAttribute(o,String(a[o]))}),t?.length&&t.forEach(o=>{let u=G(o);r.appendChild(u)}),r},V=(e,a={})=>{let r={...d,...a};return G(["svg",r,e])};var W=(...e)=>{let a=new Set;for(let t of e)if(typeof t=="string")for(let r of t.split(/\s+/))r&&a.add(r);return[...a].join(" ")};var z=e=>{if(e==null||typeof e!="object")return!1;for(let a in e)if(Object.prototype.hasOwnProperty.call(e,a)&&(a.startsWith("aria-")||a==="role"||a==="title")&&e[a]!=null)return!0;return!1};var I=e=>{let a="",t=!1;for(let r of e){if(r==="-"||r==="_"||r<=" "){t=a.length>0;continue}a.length===0?a+=r.toLowerCase():a+=t?r.toUpperCase():r,t=!1}return a};var X=e=>{let a=I(e);return a.charAt(0).toUpperCase()+a.slice(1)};var j=e=>Array.from(e.attributes).reduce((a,t)=>(a[t.name]=t.value,a),{}),N=e=>typeof e=="string"?e.split(" ").filter(Boolean):!e||!e.class?[]:e.class&&typeof e.class=="string"?e.class.split(" ").filter(Boolean):e.class&&Array.isArray(e.class)?e.class.filter(Boolean):[],n=(e,{nameAttr:a,icons:t,attrs:r})=>{let o=e.getAttribute(a);if(o==null)return;let u=X(o),f=t[u];if(!f)return console.warn(`${e.outerHTML} icon name was not found in the provided icons object.`);let l=j(e),K=z(l)?{}:{"aria-hidden":"true"},O={...d,"data-lucide":o,...K,...r,...l},Z=N(l),J=N(r),E=W("lucide",`lucide-${o}`,...Z,...J);E&&Object.assign(O,{class:E});let Q=V(f,O);return e.parentNode?.replaceChild(Q,e)};var c=[["path",{d:"M22 12h-2.48a2 2 0 0 0-1.93 1.46l-2.35 8.36a.25.25 0 0 1-.48 0L9.24 2.18a.25.25 0 0 0-.48 0l-2.35 8.36A2 2 0 0 1 4.49 12H2"}]];var C=[["path",{d:"m12 19-7-7 7-7"}],["path",{d:"M19 12H5"}]];var h=[["path",{d:"M5 12h14"}],["path",{d:"m12 5 7 7-7 7"}]];var S=[["path",{d:"M10.268 21a2 2 0 0 0 3.464 0"}],["path",{d:"M3.262 15.326A1 1 0 0 0 4 17h16a1 1 0 0 0 .74-1.673C19.41 13.956 18 12.499 18 8A6 6 0 0 0 6 8c0 4.499-1.411 5.956-2.738 7.326"}]];var g=[["path",{d:"M12 10h.01"}],["path",{d:"M12 14h.01"}],["path",{d:"M12 6h.01"}],["path",{d:"M16 10h.01"}],["path",{d:"M16 14h.01"}],["path",{d:"M16 6h.01"}],["path",{d:"M8 10h.01"}],["path",{d:"M8 14h.01"}],["path",{d:"M8 6h.01"}],["path",{d:"M9 22v-3a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v3"}],["rect",{x:"4",y:"2",width:"16",height:"20",rx:"2"}]];var p=[["path",{d:"M3 3v16a2 2 0 0 0 2 2h16"}],["path",{d:"M18 17V9"}],["path",{d:"M13 17V5"}],["path",{d:"M8 17v-3"}]];var w=[["path",{d:"M20 6 9 17l-5-5"}]];var k=[["circle",{cx:"12",cy:"12",r:"10"}],["path",{d:"M12 6v6l4 2"}]];var A=[["rect",{width:"14",height:"14",x:"8",y:"8",rx:"2",ry:"2"}],["path",{d:"M4 16c-1.1 0-2-.9-2-2V4c0-1.1.9-2 2-2h10c1.1 0 2 .9 2 2"}]];var P=[["rect",{width:"20",height:"14",x:"2",y:"5",rx:"2"}],["line",{x1:"2",x2:"22",y1:"10",y2:"10"}],["path",{d:"M6 14h2"}]];var s=[["rect",{width:"18",height:"18",x:"3",y:"3",rx:"2"}],["path",{d:"M3 9h18"}],["path",{d:"M3 15h18"}],["path",{d:"M9 3v18"}],["path",{d:"M15 3v18"}]];var m=[["path",{d:"M15 21v-8a1 1 0 0 0-1-1h-4a1 1 0 0 0-1 1v8"}],["path",{d:"M3 10a2 2 0 0 1 .709-1.528l7-6a2 2 0 0 1 2.582 0l7 6A2 2 0 0 1 21 10v9a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"}]];var L=[["polyline",{points:"22 12 16 12 14 15 10 15 8 12 2 12"}],["path",{d:"M5.45 5.11 2 12v6a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2v-6l-3.45-6.89A2 2 0 0 0 16.76 4H7.24a2 2 0 0 0-1.79 1.11z"}]];var M=[["path",{d:"m2 21 9.6-9.6"}],["path",{d:"m7.5 15.5 2.3 2.3a1 1 0 0 1 0 1.4l-2.1 2.1a1 1 0 0 1-1.4 0L4 19"}],["circle",{cx:"15.5",cy:"7.5",r:"5.5"}]];var x=[["path",{d:"M12.83 2.18a2 2 0 0 0-1.66 0L2.6 6.08a1 1 0 0 0 0 1.83l8.58 3.91a2 2 0 0 0 1.66 0l8.58-3.9a1 1 0 0 0 0-1.83z"}],["path",{d:"M2 12a1 1 0 0 0 .58.91l8.6 3.91a2 2 0 0 0 1.65 0l8.58-3.9A1 1 0 0 0 22 12"}],["path",{d:"M2 17a1 1 0 0 0 .58.91l8.6 3.91a2 2 0 0 0 1.65 0l8.58-3.9A1 1 0 0 0 22 17"}]];var B=[["rect",{width:"18",height:"11",x:"3",y:"11",rx:"2",ry:"2"}],["path",{d:"M7 11V7a5 5 0 0 1 10 0v4"}]];var D=[["path",{d:"m16 17 5-5-5-5"}],["path",{d:"M21 12H9"}],["path",{d:"M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"}]];var F=[["path",{d:"M4 5h16"}],["path",{d:"M4 12h16"}],["path",{d:"M4 19h16"}]];var q=[["path",{d:"M22 17a2 2 0 0 1-2 2H6.828a2 2 0 0 0-1.414.586l-2.202 2.202A.71.71 0 0 1 2 21.286V5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2z"}]];var y=[["path",{d:"M11 21.73a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73z"}],["path",{d:"M12 22V12"}],["polyline",{points:"3.29 7 12 12 20.71 7"}],["path",{d:"m7.5 4.27 9 5.15"}]];var R=[["path",{d:"M5 12h14"}],["path",{d:"M12 5v14"}]];var T=[["path",{d:"M9.671 4.136a2.34 2.34 0 0 1 4.659 0 2.34 2.34 0 0 0 3.319 1.915 2.34 2.34 0 0 1 2.33 4.033 2.34 2.34 0 0 0 0 3.831 2.34 2.34 0 0 1-2.33 4.033 2.34 2.34 0 0 0-3.319 1.915 2.34 2.34 0 0 1-4.659 0 2.34 2.34 0 0 0-3.32-1.915 2.34 2.34 0 0 1-2.33-4.033 2.34 2.34 0 0 0 0-3.831A2.34 2.34 0 0 1 6.35 6.051a2.34 2.34 0 0 0 3.319-1.915"}],["circle",{cx:"12",cy:"12",r:"3"}]];var b=[["path",{d:"M20 13c0 5-3.5 7.5-7.66 8.95a1 1 0 0 1-.67-.01C7.5 20.5 4 18 4 13V6a1 1 0 0 1 1-1c2 0 4.5-1.2 6.24-2.72a1.17 1.17 0 0 1 1.52 0C14.51 3.81 17 5 19 5a1 1 0 0 1 1 1z"}]];var i=[["path",{d:"M11.017 2.814a1 1 0 0 1 1.966 0l1.051 5.558a2 2 0 0 0 1.594 1.594l5.558 1.051a1 1 0 0 1 0 1.966l-5.558 1.051a2 2 0 0 0-1.594 1.594l-1.051 5.558a1 1 0 0 1-1.966 0l-1.051-5.558a2 2 0 0 0-1.594-1.594l-5.558-1.051a1 1 0 0 1 0-1.966l5.558-1.051a2 2 0 0 0 1.594-1.594z"}],["path",{d:"M20 2v4"}],["path",{d:"M22 4h-4"}],["circle",{cx:"4",cy:"20",r:"2"}]];var v=[["path",{d:"M19 21v-2a4 4 0 0 0-4-4H9a4 4 0 0 0-4 4v2"}],["circle",{cx:"12",cy:"7",r:"4"}]];var U=[["path",{d:"M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2"}],["path",{d:"M16 3.128a4 4 0 0 1 0 7.744"}],["path",{d:"M22 21v-2a4 4 0 0 0-3-3.87"}],["circle",{cx:"9",cy:"7",r:"4"}]];var H=({icons:e={},nameAttr:a="data-lucide",attrs:t={},root:r=document,inTemplates:o}={})=>{if(!Object.values(e).length)throw new Error(`Please provide an icons object.
If you want to use all the icons you can import it like:
 \`import { createIcons, icons } from 'lucide';
lucide.createIcons({icons});\``);if(typeof r>"u")throw new Error("`createIcons()` only works in a browser environment.");if(Array.from(r.querySelectorAll(`[${a}]`)).forEach(f=>n(f,{nameAttr:a,icons:e,attrs:t})),o&&Array.from(r.querySelectorAll("template")).forEach(l=>H({icons:e,nameAttr:a,attrs:t,root:l.content,inTemplates:o})),a==="data-lucide"){let f=r.querySelectorAll("[icon-name]");f.length>0&&(console.warn("[Lucide] Some icons were found with the now deprecated icon-name attribute. These will still be replaced for backwards compatibility, but will no longer be supported in v1.0 and you should switch to data-lucide"),Array.from(f).forEach(l=>n(l,{nameAttr:"icon-name",icons:e,attrs:t})))}};var Y={Activity:c,ArrowLeft:C,ArrowRight:h,BarChart3:p,Bell:S,Building:g,Check:w,Clock:k,Copy:A,CreditCard:P,Grid:s,Home:m,Inbox:L,Key:M,Layers:x,Lock:B,LogOut:D,Menu:F,MessageSquare:q,Package:y,Plus:R,Settings:T,Shield:b,Sparkles:i,User:v,Users:U},$={home:"home",inbox:"inbox",users:"users",package:"package",message:"message-square",bell:"bell",key:"key",user:"user",logout:"log-out",building:"building",layers:"layers",grid:"grid",sparkles:"sparkles",shield:"shield",check:"check",clock:"clock",creditCard:"credit-card",chart:"bar-chart-3",settings:"settings",menu:"menu",plus:"plus",arrowLeft:"arrow-left",arrowRight:"arrow-right",copy:"copy",activity:"activity",lock:"lock"};function _(){document.querySelectorAll("[data-icon]").forEach(e=>{let a=$[e.getAttribute("data-icon")];a&&(e.setAttribute("data-lucide",a),e.classList.add("mtss-icon"))}),H({icons:Y,attrs:{"aria-hidden":"true","stroke-width":1.8}})}document.addEventListener("DOMContentLoaded",_,{once:!0});})();
/*! Bundled license information:

lucide/dist/esm/defaultAttributes.mjs:
lucide/dist/esm/createElement.mjs:
lucide/dist/esm/shared/src/utils/mergeClasses.mjs:
lucide/dist/esm/shared/src/utils/hasA11yProp.mjs:
lucide/dist/esm/shared/src/utils/toCamelCase.mjs:
lucide/dist/esm/shared/src/utils/toPascalCase.mjs:
lucide/dist/esm/replaceElement.mjs:
lucide/dist/esm/icons/activity.mjs:
lucide/dist/esm/icons/arrow-left.mjs:
lucide/dist/esm/icons/arrow-right.mjs:
lucide/dist/esm/icons/bell.mjs:
lucide/dist/esm/icons/building.mjs:
lucide/dist/esm/icons/chart-column.mjs:
lucide/dist/esm/icons/check.mjs:
lucide/dist/esm/icons/clock.mjs:
lucide/dist/esm/icons/copy.mjs:
lucide/dist/esm/icons/credit-card.mjs:
lucide/dist/esm/icons/grid-3x3.mjs:
lucide/dist/esm/icons/house.mjs:
lucide/dist/esm/icons/inbox.mjs:
lucide/dist/esm/icons/key.mjs:
lucide/dist/esm/icons/layers.mjs:
lucide/dist/esm/icons/lock.mjs:
lucide/dist/esm/icons/log-out.mjs:
lucide/dist/esm/icons/menu.mjs:
lucide/dist/esm/icons/message-square.mjs:
lucide/dist/esm/icons/package.mjs:
lucide/dist/esm/icons/plus.mjs:
lucide/dist/esm/icons/settings.mjs:
lucide/dist/esm/icons/shield.mjs:
lucide/dist/esm/icons/sparkles.mjs:
lucide/dist/esm/icons/user.mjs:
lucide/dist/esm/icons/users.mjs:
lucide/dist/esm/lucide.mjs:
  (**
   * @license lucide v1.80.0 - ISC
   *
   * This source code is licensed under the ISC license.
   * See the LICENSE file in the root directory of this source tree.
   *)
*/
