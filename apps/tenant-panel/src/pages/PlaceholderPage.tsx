import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'

export function PlaceholderPage({ title, description }: { title: string; description: string }) {
  return (
    <Card>
      <CardHeader>
        <CardTitle>{title}</CardTitle>
        <CardDescription>{description}</CardDescription>
      </CardHeader>
      <CardContent className="text-sm text-muted-foreground">
        Bu sayfa PR-9&apos;da implemente edilecek.
      </CardContent>
    </Card>
  )
}
