import { Toaster as Sonner, toast } from 'sonner'
import 'sonner/dist/styles.css'

export function Toaster() {
  return (
    <Sonner
      position="top-right"
      richColors
      closeButton
      toastOptions={{
        className: 'font-sans',
        duration: 4000,
      }}
    />
  )
}

export { toast }
