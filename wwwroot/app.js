// ConviTEC frontend: JS puro, sin dependencias. Habla con el backend de su propia sede por /api/*.
const $ = s => document.querySelector(s);
const esc = s => String(s ?? '').replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
const crc = n => '₡' + Number(n || 0).toLocaleString('es-CR', {maximumFractionDigits: 0});
const fecha = f => f ? new Date(f).toLocaleString('es-CR') : '—';
const min = n => (n ?? 0) + ' min';
let sede = {};

function toast(t, err) {
  const e = $('#toast'); e.textContent = t; e.className = 'on' + (err ? ' err' : '');
  clearTimeout(toast.t); toast.t = setTimeout(() => e.className = '', 3500);
}

async function api(url, metodo = 'GET', cuerpo) {
  try {
    const r = await fetch('/api' + url, {method: metodo, headers: {'Content-Type': 'application/json'}, body: cuerpo ? JSON.stringify(cuerpo) : undefined});
    const txt = await r.text();
    const d = txt ? JSON.parse(txt) : null;
    if (!r.ok) throw new Error(d?.mensaje || d?.error || 'Error ' + r.status);
    return d;
  } catch (e) { toast(e.message === 'Failed to fetch' ? 'No hay conexión con el servidor de la sede.' : e.message, true); throw e; }
}

function tabla(cols, filas, vacio = 'No hay registros todavía.') {
  if (!filas || !filas.length) return `<div class="panel vacio">${vacio}</div>`;
  const th = cols.map(c => `<th class="${c[2] || ''}">${c[0]}</th>`).join('');
  const tr = filas.map(f => '<tr>' + cols.map(c => `<td class="${c[2] || ''}">${c[1](f)}</td>`).join('') + '</tr>').join('');
  return `<div class="panel"><table><thead><tr>${th}</tr></thead><tbody>${tr}</tbody></table></div>`;
}
const detalle = ds => (ds || []).map(d => `${esc(d.nombre || d.codigo)} × ${d.cantidad}`).join('<br>');
const tag = e => `<span class="tag ${esc(e)}">${esc(e)}</span>`;
const rutaHtml = c => (c || []).length ? '<div class="ruta">' + c.map((p, i) => `<span class="${i === c.length - 1 ? 'fin' : ''}">${esc(p)}</span>`).join('<i>→</i>') + '</div>' : '';

const colsPedido = [
  ['Pedido', p => esc(p.id)], ['Fecha', p => fecha(p.fecha)], ['Estado', p => tag(p.estado)],
  ['Destino', p => esc(p.destino)], ['Productos', p => detalle(p.detalles)],
  ['Sedes', p => esc((p.sedes || []).join(', '))], ['Monto', p => crc(p.monto), 'n']
];

const vistas = {
  catalogo: ['Catálogo global', async () => {
    const d = await api('/catalogo-global');
    const sedes = [...new Set(d.flatMap(p => Object.keys(p.existencias || {})))];
    return tabla([['Código', p => esc(p.codigo)], ['Producto', p => `<b>${esc(p.nombre)}</b><br><small>${esc(p.descripcion)}</small>`],
      ['Categoría', p => esc(p.categoria)], ['Precio', p => crc(p.precio), 'n'],
      ...sedes.map(s => [esc(s), p => p.existencias?.[s] ?? 0, 'n'])], d, 'El catálogo global está vacío.');
  }],
  inventario: ['Inventario local', async () => {
    const d = await api('/inventario');
    const opts = (await api('/catalogo-global')).map(p => `<option value="${esc(p.codigo)}">${esc(p.codigo)} · ${esc(p.nombre)}</option>`).join('');
    return `<div class="panel"><div class="fila">
      <label>Producto<select id="invCod">${opts}</select></label>
      <label>Unidades a agregar<input id="invCant" type="number" min="1" value="1"></label>
      <button class="b" id="invAdd">Agregar al inventario</button></div></div>` +
      tabla([['Código', p => esc(p.codigo)], ['Producto', p => esc(p.nombre)], ['Categoría', p => esc(p.categoria)],
        ['Precio', p => crc(p.precio), 'n'], ['Cantidad', p => p.cantidad, 'n']], d, 'Esta sede no tiene productos en inventario.');
  }, () => $('#invAdd').onclick = async () => {
    const cantidad = parseInt($('#invCant').value, 10);
    if (!(cantidad > 0)) return toast('Escriba una cantidad mayor a cero.', true);
    await api('/inventario/agregar', 'POST', {codigo: $('#invCod').value, cantidad});
    toast('Inventario actualizado.'); mostrar('inventario');
  }],
  pedido: ['Realizar pedido', async () => {
    const cat = await api('/catalogo-global'), dest = await api('/destinos');
    vistas.pedido.cat = cat; vistas.pedido.items = [];
    const po = cat.map(p => `<option value="${esc(p.codigo)}">${esc(p.nombre)} (${crc(p.precio)})</option>`).join('');
    const doo = dest.map(d => `<option>${esc(d)}</option>`).join('');
    return `<div class="panel"><div class="fila">
      <label>Producto<select id="pProd">${po}</select></label>
      <label>Cantidad<input id="pCant" type="number" min="1" value="1"></label>
      <button class="b sec" id="pAdd">Añadir al pedido</button></div></div>
      <div id="pLista"></div>
      <div class="panel"><div class="fila">
      <label>Punto de entrega<select id="pDest">${doo}</select></label>
      <button class="b" id="pEnv">Enviar pedido</button></div></div><div id="pRes"></div>`;
  }, () => {
    const V = vistas.pedido;
    const pintar = () => {
      $('#pLista').innerHTML = tabla([['Producto', i => esc(i.nombre)], ['Cantidad', i => i.cantidad, 'n'],
        ['Subtotal', i => crc(i.cantidad * i.precio), 'n'], ['', i => `<button class="b sec" data-q="${esc(i.codigo)}">Quitar</button>`]],
        V.items, 'Añada al menos un producto.');
      document.querySelectorAll('[data-q]').forEach(b => b.onclick = () => { V.items = V.items.filter(i => i.codigo !== b.dataset.q); pintar(); });
    };
    pintar();
    $('#pAdd').onclick = () => {
      const cod = $('#pProd').value, cantidad = parseInt($('#pCant').value, 10);
      if (!(cantidad > 0)) return toast('Escriba una cantidad mayor a cero.', true);
      const ya = V.items.find(i => i.codigo === cod);
      if (ya) ya.cantidad += cantidad; else { const p = V.cat.find(x => x.codigo === cod); V.items.push({codigo: cod, nombre: p.nombre, precio: p.precio, cantidad}); }
      pintar();
    };
    $('#pEnv').onclick = async () => {
      if (!V.items.length) return toast('El pedido no tiene productos.', true);
      $('#pEnv').disabled = true;
      try {
        const r = await api('/pedidos', 'POST', {destino: $('#pDest').value, items: V.items.map(i => ({codigo: i.codigo, cantidad: i.cantidad}))});
        $('#pRes').innerHTML = `<div class="msg ${r.aceptado ? 'ok' : 'mal'}">${r.aceptado ? 'Pedido aceptado. ' : 'Pedido rechazado. '}${esc(r.mensaje)}</div>`;
        if (r.aceptado) { V.items = []; pintar(); }
      } finally { $('#pEnv').disabled = false; }
    };
  }],
  pedidos: ['Historial de pedidos', async () => tabla(colsPedido, await api('/pedidos'))],
  entregas: ['Entregas pendientes', async () => {
    const p = await api('/pedidos/pendientes');
    return tabla(colsPedido.filter(c => c[0] !== 'Estado'), p, 'No hay pedidos pendientes de entrega.') +
      `<div class="panel"><div class="fila"><button class="b" id="eCalc" ${p.length ? '' : 'disabled'}>Calcular siguiente entrega</button></div><div id="eRes"></div></div>`;
  }, () => {
    const calc = $('#eCalc');
    calc.onclick = async () => {
      const r = await api('/entregas/siguiente');
      if (!r || !r.destino) { $('#eRes').innerHTML = '<div class="msg mal">No quedan destinos alcanzables desde la ubicación actual.</div>'; return; }
      $('#eRes').innerHTML = `<h2>Siguiente: ${esc(r.destino)} (${min(r.tiempo)})</h2>${rutaHtml(r.camino)}
        <p>Pedidos en esta visita: ${esc((r.pedidos || []).join(', '))}</p>
        <button class="b" id="eOk">Entregar y continuar</button>`;
      $('#eOk').onclick = async () => {
        const e = await api('/entregas/siguiente', 'POST');
        toast('Entrega registrada en ' + e.destino + '.');
        await mostrar('entregas');
        $('#eRes').innerHTML = `<div class="msg ok">Entregado en ${esc(e.destino)}. Ubicación actual: ${esc(e.ubicacionActual)}. Tiempo acumulado: ${min(e.tiempoTotal)}.${e.finalizado ? ' Recorrido finalizado.' : ''}</div>`;
      };
    };
  }],
  ventas: ['Historial de ventas', async () => tabla([
    ['Venta', v => esc(v.id)], ['Pedido', v => esc(v.pedidoId)], ['Fecha', v => fecha(v.fecha)], ['Sede', v => esc(v.sede)],
    ['Productos', v => (v.detalles || []).map(d => `${esc(d.nombre || d.codigo)} × ${d.cantidad} a ${crc(d.precio)}`).join('<br>')],
    ['Monto', v => crc(v.monto), 'n']], await api('/ventas'))],
  estadisticas: ['Estadísticas', async () => {
    const e = await api('/estadisticas'), s = e.productoEstrella;
    return `<div class="grid"><div class="panel"><h2>Producto estrella</h2>${s ?
      `<div class="cifra">${esc(s.nombre)}</div><p>${s.unidades} unidades · ${crc(s.ingresos)} en ingresos</p>` :
      '<p class="vacio">Aún no hay ventas.</p>'}</div></div><h2>Peores pedidos</h2><p>Rendimiento = monto de la venta ÷ tiempo de traslado.</p>` +
      tabla([['Pedido', p => esc(p.pedidoId)], ['Monto', p => crc(p.monto), 'n'], ['Traslado', p => min(p.tiempo), 'n'],
        ['Rendimiento', p => crc(p.rendimiento) + '/min', 'n']], e.peoresPedidos, 'Aún no hay pedidos entregados.');
  }]
};

async function mostrar(k) {
  const [titulo, cargar, enlazar] = vistas[k];
  $('#titulo').textContent = titulo;
  document.querySelectorAll('#menu button').forEach(b => b.classList.toggle('on', b.dataset.k === k));
  try { $('#vista').innerHTML = await cargar(); if (enlazar) enlazar(); }
  catch { $('#vista').innerHTML = '<div class="panel vacio">No se pudo cargar esta sección.</div>'; }
}

(async function iniciar() {
  $('#menu').innerHTML = Object.keys(vistas).map(k => `<button data-k="${k}">${vistas[k][0]}</button>`).join('');
  document.querySelectorAll('#menu button').forEach(b => b.onclick = () => mostrar(b.dataset.k));
  try { sede = await api('/sede'); $('#sedeNombre').textContent = sede.nombre; $('#sedeCodigo').textContent = 'Sede ' + sede.codigo; document.title = 'ConviTEC · ' + sede.nombre; } catch {}
  mostrar('catalogo');
})();