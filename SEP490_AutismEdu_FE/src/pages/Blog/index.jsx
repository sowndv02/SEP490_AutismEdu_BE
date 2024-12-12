import { Box, Breadcrumbs, Button, Card, CardActions, CardContent, CardMedia, Divider, FormControl, IconButton, InputAdornment, InputBase, InputLabel, OutlinedInput, Pagination, Paper, Stack, TextField, Typography } from '@mui/material'
import React, { useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import ArrowForwardIcon from '@mui/icons-material/ArrowForward';
import AccessTimeIcon from '@mui/icons-material/AccessTime';
import SearchIcon from '@mui/icons-material/Search';
import PAGES from '~/utils/pages';
import services from '~/plugins/services';
import { enqueueSnackbar } from 'notistack';
import LoadingComponent from '~/components/LoadingComponent';
import RemoveRedEyeIcon from '@mui/icons-material/RemoveRedEye';
import emptyBlog from '~/assets/images/icon/emptyblog.png'
import { format } from 'date-fns';
function Blog() {
    const nav = useNavigate();
    const [loading, setLoading] = useState(false);
    const [blogs, setBlogs] = useState([]);
    const [currentPage, setCurrentPage] = useState(1);
    const [recentBlog, setRecentBlog] = useState([]);
    const [pagination, setPagination] = useState(null);
    const [totalPage, setTotalPage] = useState(0);
    const [searchName, setSearchName] = useState("");
    useEffect(() => {
        if (pagination?.total % 10 !== 0) {
            setTotalPage(Math.floor(pagination?.total / 10) + 1);
        } else setTotalPage(Math.floor(pagination?.total / 10));
    }, [pagination])
    useEffect(() => {
        handleGetBlogs();
    }, [])

    useEffect(() => {
        handleGetBlogs();
    }, [currentPage])
    const handleGetBlogs = async () => {
        try {
            setLoading(true);
            await services.BlogAPI.getBlogs((res) => {
                if (currentPage === 1 && searchName.trim() === "") {
                    const rb = res.result.filter((r, index) => {
                        return index < 3;
                    })
                    setRecentBlog(rb);
                    res.pagination.currentSize = res.result.length
                    setPagination(res.pagination);
                }
                setBlogs(res.result);
            }, (err) => {
                console.log(err);
            }, {
                pageNumber: currentPage,
                search: searchName,
                isPublished: true
            })
            setLoading(false);
        } catch (error) {
            enqueueSnackbar("Lỗi hệ thống", { variant: "error" });
            setLoading(false);
        }
    }

    const formatDate = (date) => {
        if (!date) return "";
        const d = new Date(date);
        return `${d.getDate()}-${d.getMonth() + 1}-${d.getFullYear()}`
    }

    const handleChangePage = (event, value) => {
        setCurrentPage(Number(value));
    }

    const handleSearch = () => {
        if (currentPage !== 1) {
            setCurrentPage(1);
        } else {
            handleGetBlogs();
        }
    }
    return (
        <Box>
            <Box sx={{
                background: `linear-gradient(to bottom, #f4f4f6, transparent),linear-gradient(to right, #4468f1, #c079ea)`,
                transition: 'height 0.5s ease',
                paddingX: "140px",
                pt: "50px",
                pb: "10px",
                height: "500px"
            }}>
                <Breadcrumbs aria-label="breadcrumb" sx={{ mt: 5 }}>
                    <Link underline="hover" color="inherit" to={PAGES.ROOT + PAGES.HOME}>
                        Trang chủ
                    </Link>
                    <Typography sx={{ color: 'text.primary' }}>Danh sách blog</Typography>
                </Breadcrumbs>
                <Typography variant='h2' mt={5}>Blog</Typography>
                {(!blogs || blogs.length === 0) &&
                    <Stack width="100%" alignItems="center" justifyContent="center">
                        <Box sx={{ textAlign: "center" }}>
                            <img src={emptyBlog} style={{ height: "200px" }} />
                            <Typography mt={5} color="black">Không có bài viết nào!</Typography>
                        </Box>
                    </Stack>
                }
            </Box>
            <Stack direction='row' sx={{ width: "80%", margin: "auto", position: "relative", top: "-200px" }} justifyContent="space-between">
                <Box sx={{ width: '60%' }}>
                    {
                        blogs && blogs[0] && (
                            <Card sx={{
                                paddingBottom: '20px',
                                transition: "transform 0.3s ease, box-shadow 0.3s ease",
                                '&:hover': {
                                    transform: "scale(1.02) translateY(-10px)",
                                    boxShadow: "0 8px 16px rgba(0, 0, 0, 0.2)",
                                    cursor: "pointer"
                                }
                            }}>
                                <CardMedia
                                    component="img"
                                    sx={{ height: "70%" }}
                                    image={blogs[0].urlImageDisplay}
                                    alt="Live from space album cover"
                                />
                                <CardContent sx={{ flex: '1 0 auto' }}>
                                    <Typography component="div" variant="h4" sx={{
                                        whiteSpace: "break-spaces", wordBreak: 'break-word'
                                    }}>
                                        {blogs[0].title}
                                    </Typography>
                                    <Typography component="div" sx={{
                                        whiteSpace: "break-spaces", wordBreak: 'break-word'
                                    }}>
                                        {blogs[0].description}
                                    </Typography>
                                    <Stack direction='row' gap={5}>
                                        <Stack direction='row' mt={2} gap={1}>
                                            <AccessTimeIcon /> <Typography>{format(blogs[0]?.publishDate, 'dd/MM/yyyy')}</Typography>
                                        </Stack>
                                        <Stack direction='row' mt={2} gap={1}>
                                            <RemoveRedEyeIcon /> <Typography>{blogs[0].viewCount}</Typography>
                                        </Stack>
                                    </Stack>
                                </CardContent>
                                <CardActions>
                                    <Button sx={{ fontSize: "20px" }} endIcon={<ArrowForwardIcon />}
                                        onClick={() => nav(PAGES.ROOT + PAGES.BLOG_LIST + `/${blogs[0].id}`)}
                                    >Tìm hiểu thêm </Button>
                                </CardActions>
                            </Card>
                        )
                    }
                    {
                        blogs && blogs.length > 1 && blogs.map((b, index) => {
                            if (index > 0) {
                                return (
                                    <Card key={b.id} sx={{
                                        display: 'flex',
                                        mt: 5,
                                        transition: "transform 0.3s ease, box-shadow 0.3s ease",
                                        '&:hover': {
                                            cursor: "pointer",
                                            transform: "scale(1.02) translateY(-10px)",
                                            boxShadow: "0 8px 16px rgba(0, 0, 0, 0.2)"
                                        }
                                    }}>
                                        <CardMedia
                                            component="img"
                                            sx={{ width: "40%" }}
                                            image={b.urlImageDisplay}
                                            alt="Live from space album cover"
                                        />
                                        <Box sx={{ width: "60%", height: "100%", display: "flex", alignItems: "center" }}>
                                            <CardContent sx={{ width: "100%" }}>
                                                <Typography component="div" variant="h5" sx={{
                                                    width: "100%",
                                                    whiteSpace: "break-spaces", wordBreak: 'break-word'
                                                }}>
                                                    {b.title}
                                                </Typography>
                                                <Typography component="div" sx={{
                                                    whiteSpace: "break-spaces", wordBreak: 'break-word'
                                                }}>
                                                    {b.description}
                                                </Typography>
                                                <Button sx={{ fontSize: "20px" }} endIcon={<ArrowForwardIcon />} onClick={() => nav(PAGES.ROOT + PAGES.BLOG_LIST + `/${b.id}`)}>Tìm hiểu thêm </Button>
                                            </CardContent>
                                        </Box>
                                    </Card>
                                )
                            } else return null;
                        })
                    }
                    {
                        blogs.length !== 0 && (
                            <Pagination count={totalPage || 1} page={currentPage} color="secondary" sx={{ mt: 5 }} onChange={handleChangePage} />
                        )
                    }
                </Box>


                {
                    recentBlog.length !== 0 && (
                        <>
                            <Box sx={{
                                border: "5px solid #c09de8", width: "30%",
                                backgroundColor: "white", borderRadius: "5px",
                                p: 2,
                                alignSelf: 'flex-start'
                            }}>
                                <FormControl sx={{ width: '100%' }} variant="outlined">
                                    <OutlinedInput
                                        placeholder='Tìm kiếm ...'
                                        value={searchName}
                                        onChange={(e) => setSearchName(e.target.value)}
                                        endAdornment={
                                            <InputAdornment position="end">
                                                <IconButton
                                                    edge="end"
                                                    onClick={handleSearch}
                                                >
                                                    <SearchIcon />
                                                </IconButton>
                                            </InputAdornment>
                                        }
                                    />
                                </FormControl>
                                <Typography variant='h5' mt={5}>Bài viết gần đây</Typography>
                                <Divider sx={{ width: "100%", mt: 2, backgroundColor: "gray" }} />
                                {
                                    recentBlog && recentBlog.length !== 0 && recentBlog.map((r) => {
                                        return (
                                            <Link to={PAGES.ROOT + PAGES.BLOG_LIST + `/${r.id}`} key={r.id}>
                                                <Stack direction='row' gap={2} sx={{ cursor: "pointer", mt: 2 }}>
                                                    <Box sx={{
                                                        backgroundImage: `url('${r.urlImageDisplay}')`,
                                                        width: "70px",
                                                        height: "70px",
                                                        backgroundSize: "cover",
                                                        backgroundPosition: "center",
                                                        borderRadius: "10px"
                                                    }}>
                                                    </Box>
                                                    <Box sx={{ width: "70%" }}>
                                                        <Typography sx={{
                                                            fontSize: "16px", display: '-webkit-box',
                                                            WebkitLineClamp: 1,
                                                            WebkitBoxOrient: 'vertical',
                                                            overflow: 'hidden',
                                                            textOverflow: 'ellipsis',
                                                            whiteSpace: "break-spaces", wordBreak: 'break-word'
                                                        }}>
                                                            {r.title}
                                                        </Typography>
                                                        <Stack direction='row' gap={5}>
                                                            <Stack direction='row' mt={2} gap={1}>
                                                                <AccessTimeIcon /> <Typography>{format(r.publishDate, 'dd/MM/yyyy')}</Typography>
                                                            </Stack>
                                                            <Stack direction='row' mt={2} gap={1}>
                                                                <RemoveRedEyeIcon /> <Typography>{r.viewCount}</Typography>
                                                            </Stack>
                                                        </Stack>
                                                    </Box>
                                                </Stack>
                                            </Link>
                                        )
                                    })
                                }
                            </Box>
                        </>
                    )
                }
            </Stack>
            <LoadingComponent open={loading} />
        </Box>
    )
}

export default Blog
