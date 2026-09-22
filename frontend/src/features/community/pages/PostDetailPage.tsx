import { Link, useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { deletePost, getPost } from '../api/communityApi'
import { CommunityHeader, ErrorNotice, secondaryClass } from '../components/CommunityUI'
import { useCommunityIdentity } from '../communitySupport'
import { PostCard } from '../components/PostCard'
import { LoadingState } from '../../../shared/components/QueryStates'
import { useAuthStore } from '../../auth/authStore'

export default function PostDetailPage() {
  const { id } = useParams()
  const identity = useCommunityIdentity()
  const admin = useAuthStore(s => s.user?.role === 'Admin')
  const navigate = useNavigate()
  const client = useQueryClient()
  const query = useQuery({ queryKey: ['community', identity, 'posts', Number(id)], queryFn: ({ signal }) => getPost(Number(id), signal) })
  const deletion = useMutation({ networkMode: 'always', mutationFn: () => deletePost(Number(id)), onSuccess: async () => { await client.invalidateQueries({ queryKey: ['community'] }); navigate('/community', { replace: true }) } })
  return <section className="p-4">
    <CommunityHeader title="En el tablón" />
    {query.isLoading && <LoadingState />}<ErrorNotice error={query.error || deletion.error} />
    {query.data && <>
      <PostCard post={query.data} detail />
      {admin && <div className="mt-4 flex flex-wrap gap-2">
        {query.data.kind !== 'achievement' && <Link className={secondaryClass} to={`/admin/community/posts/${id}/edit`}>Editar publicación</Link>}
        <button className={`${secondaryClass} text-danger`} disabled={deletion.isPending} onClick={() => { if (confirm('¿Retirar esta publicación del tablón?')) deletion.mutate() }}>Retirar publicación</button>
      </div>}
    </>}
  </section>
}
