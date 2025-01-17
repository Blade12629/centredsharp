// Generated by Sichem at 2/18/2021 7:29:26 PM

using System.Runtime.InteropServices;

namespace CentrED.Renderer.StbRectPack
{
	unsafe partial class StbRectPack
	{
		public const int STBRP_HEURISTIC_Skyline_default = 0;
		public const int STBRP_HEURISTIC_Skyline_BL_sortHeight = STBRP_HEURISTIC_Skyline_default;
		public const int STBRP_HEURISTIC_Skyline_BF_sortHeight = 2;
		public const int STBRP__INIT_skyline = 1;

		[StructLayout(LayoutKind.Sequential)]
		public struct stbrp_rect
		{
			public int id;
			public int w;
			public int h;
			public int x;
			public int y;
			public int was_packed;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct stbrp_node
		{
			public int x;
			public int y;
			public stbrp_node* next;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct stbrp__findresult
		{
			public int x;
			public int y;
			public stbrp_node** prev_link;
		}

		public static void stbrp_setup_heuristic(stbrp_context* context, int heuristic)
		{
			switch (context->init_mode)
			{
				case STBRP__INIT_skyline:
					;
					context->heuristic = heuristic;
					break;
				default:
					throw new Exception("Mode " + context->init_mode + " is not supported.");
			}
		}

		public static void stbrp_setup_allow_out_of_mem(stbrp_context* context, int allow_out_of_mem)
		{
			if (allow_out_of_mem != 0)
				context->align = 1;
			else
				context->align = (context->width + context->num_nodes - 1) / context->num_nodes;
		}

		public static void stbrp_init_target(stbrp_context* context, int width, int height, stbrp_node* nodes,
			int num_nodes)
		{
			var i = 0;
			for (i = 0; i < num_nodes - 1; ++i)
				nodes[i].next = &nodes[i + 1];
			nodes[i].next = null;
			context->init_mode = STBRP__INIT_skyline;
			context->heuristic = STBRP_HEURISTIC_Skyline_default;
			context->free_head = &nodes[0];
			context->active_head = &context->extra[0];
			context->width = width;
			context->height = height;
			context->num_nodes = num_nodes;
			stbrp_setup_allow_out_of_mem(context, 0);
			context->extra[0].x = 0;
			context->extra[0].y = 0;
			context->extra[0].next = &context->extra[1];
			context->extra[1].x = (int)width;
			context->extra[1].y = 65535;
			context->extra[1].next = null;
		}

		public static int stbrp__skyline_find_min_y(stbrp_context* c, stbrp_node* first, int x0, int width, int* pwaste)
		{
			var node = first;
			var x1 = x0 + width;
			var min_y = 0;
			var visited_width = 0;
			var waste_area = 0;
			min_y = 0;
			waste_area = 0;
			visited_width = 0;
			while (node->x < x1)
			{
				if (node->y > min_y)
				{
					waste_area += visited_width * (node->y - min_y);
					min_y = node->y;
					if (node->x < x0)
						visited_width += node->next->x - x0;
					else
						visited_width += node->next->x - node->x;
				}
				else
				{
					var under_width = node->next->x - node->x;
					if (under_width + visited_width > width)
						under_width = width - visited_width;
					waste_area += under_width * (min_y - node->y);
					visited_width += under_width;
				}

				node = node->next;
			}

			*pwaste = waste_area;
			return min_y;
		}

		public static stbrp__findresult stbrp__skyline_find_best_pos(stbrp_context* c, int width, int height)
		{
			var best_waste = 1 << 30;
			var best_x = 0;
			var best_y = 1 << 30;
			var fr = new stbrp__findresult();
			stbrp_node** prev;
			stbrp_node* node;
			stbrp_node* tail;
			stbrp_node** best = null;
			width = width + c->align - 1;
			width -= width % c->align;
			if (width > c->width || height > c->height)
			{
				fr.prev_link = null;
				fr.x = fr.y = 0;
				return fr;
			}

			node = c->active_head;
			prev = &c->active_head;
			while (node->x + width <= c->width)
			{
				var y = 0;
				var waste = 0;
				y = stbrp__skyline_find_min_y(c, node, node->x, width, &waste);
				if (c->heuristic == STBRP_HEURISTIC_Skyline_BL_sortHeight)
				{
					if (y < best_y)
					{
						best_y = y;
						best = prev;
					}
				}
				else
				{
					if (y + height <= c->height)
						if (y < best_y || y == best_y && waste < best_waste)
						{
							best_y = y;
							best_waste = waste;
							best = prev;
						}
				}

				prev = &node->next;
				node = node->next;
			}

			best_x = best == null ? 0 : (*best)->x;
			if (c->heuristic == STBRP_HEURISTIC_Skyline_BF_sortHeight)
			{
				tail = c->active_head;
				node = c->active_head;
				prev = &c->active_head;
				while (tail->x < width)
					tail = tail->next;
				while (tail != null)
				{
					var xpos = tail->x - width;
					var y = 0;
					var waste = 0;
					while (node->next->x <= xpos)
					{
						prev = &node->next;
						node = node->next;
					}

					y = stbrp__skyline_find_min_y(c, node, xpos, width, &waste);
					if (y + height <= c->height)
						if (y <= best_y)
							if (y < best_y || waste < best_waste || waste == best_waste && xpos < best_x)
							{
								best_x = xpos;
								best_y = y;
								best_waste = waste;
								best = prev;
							}

					tail = tail->next;
				}
			}

			fr.prev_link = best;
			fr.x = best_x;
			fr.y = best_y;
			return fr;
		}

		public static stbrp__findresult stbrp__skyline_pack_rectangle(stbrp_context* context, int width, int height)
		{
			var res = stbrp__skyline_find_best_pos(context, width, height);
			stbrp_node* node;
			stbrp_node* cur;
			if (res.prev_link == null || res.y + height > context->height || context->free_head == null)
			{
				res.prev_link = null;
				return res;
			}

			node = context->free_head;
			node->x = (int)res.x;
			node->y = (int)(res.y + height);
			context->free_head = node->next;
			cur = *res.prev_link;
			if (cur->x < res.x)
			{
				var next = cur->next;
				cur->next = node;
				cur = next;
			}
			else
			{
				*res.prev_link = node;
			}

			while (cur->next != null && cur->next->x <= res.x + width)
			{
				var next = cur->next;
				cur->next = context->free_head;
				context->free_head = cur;
				cur = next;
			}

			node->next = cur;
			if (cur->x < res.x + width)
				cur->x = (int)(res.x + width);
			return res;
		}

		public static int rect_height_compare(void* a, void* b)
		{
			var p = (stbrp_rect*)a;
			var q = (stbrp_rect*)b;
			if (p->h > q->h)
				return -1;
			if (p->h < q->h)
				return 1;
			return p->w > q->w ? -1 : p->w < q->w ? 1 : 0;
		}

		public static int rect_original_order(void* a, void* b)
		{
			var p = (stbrp_rect*)a;
			var q = (stbrp_rect*)b;
			return p->was_packed < q->was_packed ? -1 : p->was_packed > q->was_packed ? 1 : 0;
		}

		public static int stbrp_pack_rects(stbrp_context* context, stbrp_rect* rects, int num_rects)
		{
			var i = 0;
			var all_rects_packed = 1;
			for (i = 0; i < num_rects; ++i)
				rects[i].was_packed = i;
			CRuntime.qsort(rects, (ulong)num_rects, (ulong)sizeof(stbrp_rect), rect_height_compare);
			for (i = 0; i < num_rects; ++i)
				if (rects[i].w == 0 || rects[i].h == 0)
				{
					rects[i].x = rects[i].y = 0;
				}
				else
				{
					var fr = stbrp__skyline_pack_rectangle(context, rects[i].w, rects[i].h);
					if (fr.prev_link != null)
					{
						rects[i].x = (int)fr.x;
						rects[i].y = (int)fr.y;
					}
					else
					{
						rects[i].x = rects[i].y = 0xffff;
					}
				}

			CRuntime.qsort(rects, (ulong)num_rects, (ulong)sizeof(stbrp_rect), rect_original_order);
			for (i = 0; i < num_rects; ++i)
			{
				rects[i].was_packed = rects[i].x == 0xffff && rects[i].y == 0xffff ? 0 : 1;
				if (rects[i].was_packed == 0)
					all_rects_packed = 0;
			}

			return all_rects_packed;
		}
	}
}