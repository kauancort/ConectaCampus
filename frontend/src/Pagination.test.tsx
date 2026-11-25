// @vitest-environment jsdom
import { afterEach, expect, it } from 'vitest'
import { cleanup, fireEvent, render, screen } from '@testing-library/react'
import Pagination, { usePagination } from './Pagination'

afterEach(cleanup)
function List({ values }: { values: number[] }) {
  const pages = usePagination(values, 2)
  return <><div>{pages.items.join(', ')}</div><Pagination label="exemplo" {...pages} /></>
}
it('ajusta a página quando a lista diminui e permite voltar', () => {
  const { rerender } = render(<List values={[1, 2, 3, 4, 5]} />)
  fireEvent.click(screen.getByRole('button', { name: 'Próxima →' }))
  fireEvent.click(screen.getByRole('button', { name: 'Próxima →' }))
  expect(screen.getByText('Página 3 de 3')).toBeTruthy()
  rerender(<List values={[1, 2, 3]} />)
  expect(screen.getByText('Página 2 de 2')).toBeTruthy()
  expect(screen.getByText('3')).toBeTruthy()
  fireEvent.click(screen.getByRole('button', { name: '← Anterior' }))
  expect(screen.getByText('1, 2')).toBeTruthy()
})
