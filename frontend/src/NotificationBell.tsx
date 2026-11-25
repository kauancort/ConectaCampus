import { useCallback, useEffect, useId, useRef, useState } from 'react'
import { useAuth } from './auth'
import useDismissiblePopover from './useDismissiblePopover'

type Notification = { id: string; mensagem: string; lida: boolean; criadaEmUtc?: string }

function dateLabel(value?: string) {
  if (!value) return ''
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? '' : date.toLocaleDateString('pt-BR', { day: '2-digit', month: 'short', year: 'numeric' })
}

export default function NotificationBell({ open, onToggle, onClose }: { open: boolean; onToggle: () => void; onClose: () => void }) {
  const { api } = useAuth()
  const [items, setItems] = useState<Notification[]>([])
  const panelId = useId()
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [marking, setMarking] = useState<string | null>(null)
  const menu = useRef<HTMLDivElement>(null)
  const trigger = useRef<HTMLButtonElement>(null)
  const requestVersion = useRef(0)
  const mounted = useRef(false)

  const load = useCallback(async () => {
    const version = ++requestVersion.current
    try {
      const result = await api<Notification[]>('/api/notificacoes')
      if (mounted.current && version === requestVersion.current) { setItems(result); setError('') }
    } catch (e) {
      if (mounted.current && version === requestVersion.current) setError(e instanceof Error ? e.message : 'Não foi possível carregar as notificações.')
    } finally {
      if (mounted.current && version === requestVersion.current) setLoading(false)
    }
  }, [api])

  useEffect(() => {
    mounted.current = true
    void load()
    const timer = window.setInterval(() => void load(), 60000)
    const refresh = () => { void load() }
    window.addEventListener('notifications-changed', refresh)
    window.addEventListener('focus', refresh)
    return () => { mounted.current = false; requestVersion.current++; window.clearInterval(timer); window.removeEventListener('notifications-changed', refresh); window.removeEventListener('focus', refresh) }
  }, [load])

  useDismissiblePopover(open, onClose, menu, trigger)

  async function markRead(item: Notification) {
    if (marking) return
    setMarking(item.id); setError('')
    try {
      await api(`/api/notificacoes/${encodeURIComponent(item.id)}/lida`, { method: 'POST' })
      if (mounted.current) setItems(current => current.map(notification => notification.id === item.id ? { ...notification, lida: true } : notification))
      window.dispatchEvent(new Event('notifications-changed'))
    } catch (e) {
      if (mounted.current) setError(e instanceof Error ? e.message : 'Não foi possível atualizar a notificação.')
    } finally { if (mounted.current) setMarking(null) }
  }

  const unread = items.filter(item => !item.lida).length
  const label = unread ? `Notificações, ${unread} ${unread === 1 ? 'não lida' : 'não lidas'}` : 'Notificações'
  return <div className="notification-menu" ref={menu}>
    <button ref={trigger} type="button" className="notification-trigger" aria-label={label} aria-expanded={open} aria-controls={panelId} onClick={() => { onToggle(); if (!open) void load() }}>
      <svg aria-hidden="true" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round"><path d="M18 8.5a6 6 0 0 0-12 0c0 6-2.5 6.5-2.5 8h17c0-1.5-2.5-2-2.5-8Z" /><path d="M10 19.5a2.2 2.2 0 0 0 4 0" /></svg>
      {unread > 0 && <span className="notification-badge" aria-hidden="true">{unread > 99 ? '99+' : unread}</span>}
    </button>
    {open && <section className="notification-popover" id={panelId} role="region" aria-label="Notificações recentes" data-popover-panel>
      <div className="notification-popover-head"><div><h2>Notificações</h2></div><div className="notification-head-actions"><span className="notification-popover-count">{unread} {unread === 1 ? 'não lida' : 'não lidas'}</span><button type="button" className="notification-close" aria-label="Fechar notificações" onClick={() => { onClose(); trigger.current?.focus() }}>×</button></div></div>
      {error && <div className="notification-popover-error" role="alert">{error} <button className="text-button" type="button" onClick={() => void load()}>Tentar novamente</button></div>}
      <div className="notification-popover-list">
        {loading ? <p className="notification-popover-empty" role="status">Carregando notificações…</p> : items.length === 0 ? (error ? null : <div className="notification-popover-empty"><span aria-hidden="true">✳</span><p>Nenhuma notificação ainda.</p></div>) : items.map(item => <article className={`notification-popover-item ${item.lida ? '' : 'unread'}`} key={item.id}>
          <span className="notification-dot" aria-hidden="true" /><div><p>{item.mensagem}</p>{dateLabel(item.criadaEmUtc) && <time dateTime={item.criadaEmUtc}>{dateLabel(item.criadaEmUtc)}</time>}{!item.lida && <button className="notification-read" type="button" aria-label={`Marcar como lida: ${item.mensagem}`} disabled={marking !== null} onClick={() => void markRead(item)}>{marking === item.id ? 'Atualizando…' : 'Marcar como lida'}</button>}</div>
        </article>)}
      </div>
    </section>}
  </div>
}
