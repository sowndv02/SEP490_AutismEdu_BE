import { Box, Button, IconButton, MenuItem, Paper, Select, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, TextField, Typography } from '@mui/material';
import { enqueueSnackbar } from 'notistack';
import { useEffect, useState } from 'react';
import TablePagging from '~/components/TablePagging';
import services from '~/plugins/services';
import VisibilityIcon from '@mui/icons-material/Visibility';
import ConfirmDialog from '~/components/ConfirmDialog';
import LoadingComponent from '~/components/LoadingComponent';
import BorderColorIcon from '@mui/icons-material/BorderColor';
import { useNavigate } from 'react-router-dom';
import PAGES from '~/utils/pages';
import BlogDetail from './BlogDetail';
function BlogManagement() {
    const [blogs, setBlogs] = useState([]);
    const [loading, setLoading] = useState(false);
    const [status, setStatus] = useState("all");
    const [searchName, setSearchName] = useState("");
    const [pagination, setPagination] = useState(null);
    const [currentPage, setCurrentPage] = useState(1);
    const [currentBlog, setCurrentBlog] = useState(null)
    const [openConfirm, setOpenConfirm] = useState(false);
    const [orderBy, setOrderBy] = useState("publishDate");
    const [sort, setSort] = useState("desc");
    const nav = useNavigate();
    const [openDetail, setOpenDetail] = useState(false);
    useEffect(() => {
        handleGetBlogs();
    }, [])
    useEffect(() => {
        handleGetBlogs();
    }, [currentPage])
    useEffect(() => {
        if (currentPage === 1) {
            handleGetBlogs();
        } else {
            setCurrentPage(1)
        }
    }, [status, orderBy, sort])

    useEffect(() => {
        const handler = setTimeout(() => {
            if (currentPage === 1) {
                handleGetBlogs();
            } else {
                setCurrentPage(1)
            }
        }, 1000)
        return () => {
            clearTimeout(handler)
        }
    }, [searchName])
    const formatDate = (date) => {
        if (!date) return "";
        const d = new Date(date);
        return `${d.getDate()}/${d.getMonth() + 1}/${d.getFullYear()}`
    }
    const handleGetBlogs = async () => {
        try {
            setLoading(true);
            await services.BlogAPI.getBlogs((res) => {
                setBlogs(res.result);
                res.pagination.currentSize = res.result.length
                setPagination(res.pagination);
            }, (err) => {
                console.log(err);
            }, {
                pageNumber: currentPage,
                isPublished: status,
                orderBy: orderBy,
                sort: sort,
                search: searchName
            })
            setLoading(false);
        } catch (error) {
            enqueueSnackbar("Lỗi hệ thống", { variant: "error" });
            setLoading(false);
        }
    }

    const changeStatus = async () => {
        try {
            await services.BlogAPI.updateBlogStatus(currentBlog.id, {
                id: currentBlog.id,
                isActive: currentBlog.isPublished ? false : true
            },
                (res) => {
                    if (status !== "all") {
                        const filterUpdate = blogs.filter((p) => {
                            return p.id !== currentBlog.id;
                        })
                        setBlogs(filterUpdate);
                    }
                    else {
                        const filterUpdate = blogs.map((p) => {
                            if (p.id !== currentBlog.id) {
                                return p;
                            } else return res.result;
                        })
                        setBlogs(filterUpdate);
                    }
                    enqueueSnackbar("Cập nhật thành công", { variant: "success" })
                }, (error) => {
                    enqueueSnackbar(error.error[0], { variant: "error" })
                })
            setOpenConfirm(false);
        } catch (error) {
            enqueueSnackbar("Cập nhật thất bại", { variant: "error" })
            setOpenConfirm(false);
        }
    }
    return (
        <Paper variant='elevation' sx={{ p: 3 }}>
            <Typography variant='h4'>Quản lý bài viết</Typography>
            <Box sx={{ display: "flex", gap: 5 }} mt={5}>
                <Select value={status} onChange={(e) => setStatus(e.target.value)}>
                    <MenuItem value="all">Tất cả</MenuItem>
                    <MenuItem value="true">Đang hiện</MenuItem>
                    <MenuItem value="false">Đang ẩn</MenuItem>
                </Select>
                <Select value={orderBy} onChange={(e) => setOrderBy(e.target.value)}>
                    <MenuItem value="publishDate">Ngày đăng</MenuItem>
                    <MenuItem value="createdDate">Ngày tạo</MenuItem>
                    <MenuItem value="title">Tiêu đề</MenuItem>
                </Select>
                <Select value={sort} onChange={(e) => setSort(e.target.value)}>
                    <MenuItem value="asc">Tăng dần</MenuItem>
                    <MenuItem value="desc">Giảm dần</MenuItem>
                </Select>
                <TextField
                    label="Tìm kiếm theo tiêu đề"
                    value={searchName}
                    onChange={(e) => setSearchName(e.target.value)}
                />
            </Box>
            <TableContainer component={Paper} sx={{ mt: 5 }}>
                <Table sx={{ minWidth: 650 }}>
                    <TableHead>
                        <TableRow>
                            <TableCell>STT</TableCell>
                            <TableCell sx={{ maxWidth: "200px" }}>Tiêu đề</TableCell>
                            <TableCell align="center">Số người xem</TableCell>
                            <TableCell align="center">Ngày tạo</TableCell>
                            <TableCell align="center">Người tao</TableCell>
                            <TableCell>Hành động</TableCell>
                        </TableRow>
                    </TableHead>
                    <TableBody>
                        {
                            blogs.length !== 0 && blogs.map((b, index) => {
                                return (
                                    <TableRow key={b.id}>
                                        <TableCell>
                                            {index + 1 + (currentPage - 1) * 10}
                                        </TableCell>
                                        <TableCell sx={{ maxWidth: "200px" }}>
                                            <Typography sx={{ whiteSpace: "break-spaces", wordBreak: 'break-word' }}>{b.title}</Typography>
                                        </TableCell>
                                        <TableCell align="center">
                                            {b.viewCount}
                                        </TableCell>
                                        <TableCell align="center">
                                            {formatDate(b.createdDate)}
                                        </TableCell>
                                        <TableCell align="center">
                                            {b.author.email}
                                        </TableCell>
                                        <TableCell>
                                            <IconButton sx={{ color: "#5fc35f" }}
                                                onClick={() => { setOpenDetail(true); setCurrentBlog(b) }}
                                            ><VisibilityIcon /></IconButton>
                                            <IconButton sx={{ color: "#ffc427" }} onClick={() => { nav(PAGES.BLOG_MANAGEMENT + '/edit/' + b.id) }}><BorderColorIcon /></IconButton>
                                            {
                                                b.isPublished ? (
                                                    <Button variant='outlined' sx={{ color: "red", borderColor: "red", ml: 1 }}
                                                        onClick={() => { setOpenConfirm(true); setCurrentBlog(b) }}
                                                    >Ẩn</Button>
                                                ) : (
                                                    <Button variant='outlined' sx={{ color: "blue", borderColor: "blue", ml: 1 }}
                                                        onClick={() => { setOpenConfirm(true); setCurrentBlog(b) }}
                                                    >Hiện</Button>
                                                )
                                            }

                                        </TableCell>
                                    </TableRow>
                                )
                            })
                        }
                    </TableBody>
                </Table>
                <TablePagging pagination={pagination} setPagination={setPagination} setCurrentPage={setCurrentPage} />
            </TableContainer>
            <LoadingComponent open={loading} />
            {
                <ConfirmDialog openConfirm={openConfirm}
                    setOpenConfirm={setOpenConfirm}
                    title="Đổi trạng thái"
                    handleAction={changeStatus}
                    content={`Bạn có chắc muốn ${currentBlog && currentBlog.isPublished ? "ẩn" : "hiện"} bài viết này không`} />
            }
            <BlogDetail openDetail={openDetail} setOpenDetail={setOpenDetail} blog={currentBlog} />
        </Paper>

    )
}

export default BlogManagement
