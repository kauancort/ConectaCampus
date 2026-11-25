import { useEffect, useState } from 'react'

export function usePagination<T>(items: T[], pageSize: number) {
  const [page, setPage] = useState(1)
  const pages = Math.max(1, Math.ceil(items.length / pageSize))
  const currentPage = Math.min(page, pages)
  useEffect(() => { setPage(current => Math.min(current, pages)) }, [pages])
  return { items: items.slice((currentPage - 1) * pageSize, currentPage * pageSize), page: currentPage, pages, total: items.length, pageSize, setPage }
}

type PaginationProps = { label: string; page: number; pages: number; total: number; pageSize: number; setPage: (page: number) => void }
export default function Pagination({ label, page, pages, total, pageSize, setPage }: PaginationProps) {
  if (pages <= 1) return null
  return <nav className="pagination" aria-label={`Paginação: ${label}`}>
    <span className="pagination-count" aria-live="polite">Mostrando {(page - 1) * pageSize + 1}–{Math.min(page * pageSize, total)} de {total}</span>
    <div className="pagination-controls"><button className="button secondary small" type="button" disabled={page === 1} onClick={() => setPage(page - 1)}>← Anterior</button><span aria-live="polite">Página {page} de {pages}</span><button className="button secondary small" type="button" disabled={page === pages} onClick={() => setPage(page + 1)}>Próxima →</button></div>
  </nav>
}
