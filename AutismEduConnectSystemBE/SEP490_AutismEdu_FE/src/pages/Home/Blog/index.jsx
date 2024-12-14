import ArrowForwardIcon from '@mui/icons-material/ArrowForward';
import { Box, Button, Card, CardActions, CardContent, CardMedia, Chip, Grid, Stack, Typography } from '@mui/material';
import { enqueueSnackbar } from 'notistack';
import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import ButtonIcon from '~/components/ButtonComponent/ButtonIcon';
import services from '~/plugins/services';
import PAGES from '~/utils/pages';
function Blog() {
    const [blogs, setBlogs] = useState([]);
    const nav = useNavigate();
    useEffect(() => {
        handleGetBlogs();
    }, [])
    const handleGetBlogs = async () => {
        try {
            await services.BlogAPI.getBlogs((res) => {
                const rb = res.result.filter((r, index) => {
                    return index < 4;
                })
                setBlogs(rb);
            }, (err) => {
                console.log(err);
            }, {
                isPublished: true
            })
        } catch (error) {
            enqueueSnackbar("Lỗi hệ thống", { variant: "error" });
        }
    }
    return (
        <Box sx={{ width: "100vw", py: "100px", textAlign: 'center', bgcolor: "#b08fd8" }}>
            <Stack direction='row' sx={{ justifyContent: "center", width: "100vw" }}>
                <Box sx={{
                    width: {
                        xl: "80%",
                        lg: "90%"
                    }
                }}>
                    <Grid container m={0} mb={5} spacing={{ xs: 2, md: 3 }} columns={{ xs: 4, sm: 8, md: 12 }} textAlign={"left"}>
                        <Grid item xs={12} md={6} >
                            <Chip
                                label={"BÀI ĐĂNG"}
                                sx={{
                                    fontSize: '14px',
                                    padding: '20px 10px',
                                    bgcolor: "#f5e7f1",
                                    fontWeight: "bold",
                                    color: "#DB7093"
                                }}
                            />
                            <Typography variant='h3' sx={{ fontSize: "44px", color: "#192335", fontWeight: "bold" }} mt={3}>
                                Bài Viết Mới Nhất.
                            </Typography>
                        </Grid>
                        <Grid item xs={12} md={6} textAlign={"right"}>
                            <ButtonIcon text={"ĐỌC THÊM"} action={() => { nav(PAGES.ROOT + PAGES.BLOG_LIST) }} />
                        </Grid>
                    </Grid>
                    {
                        blogs.length === 0 && <Typography>Chưa có bài viết nào.</Typography>
                    }
                    <Grid container spacing={{ xs: 2, md: 3 }} columns={{ xs: 4, sm: 8, md: 12 }} textAlign={"left"}>
                        {
                            blogs.length !== 0 && (
                                <Grid item xs={12} md={6}>
                                    <Card sx={{
                                        height: "100%",
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
                                            sx={{ height: "60%" }}
                                            image={blogs[0] ? blogs[0].urlImageDisplay : "/"}
                                            alt="Live from space album cover"
                                        />
                                        <Box sx={{ display: 'flex', flexDirection: 'column', height: "40%" }}>
                                            <CardContent sx={{ flex: '1 0 auto' }}>
                                                <Typography component="div" variant="h4" sx={{ whiteSpace: "break-spaces", wordBreak: 'break-word' }}>
                                                    {blogs[0] ? blogs[0].title : ""}
                                                </Typography>
                                                <Typography component="div" sx={{
                                                    whiteSpace: "break-spaces", wordBreak: 'break-word',
                                                    display: '-webkit-box',
                                                    WebkitLineClamp: 2,
                                                    WebkitBoxOrient: 'vertical',
                                                    overflow: 'hidden',
                                                    textOverflow: 'ellipsis'
                                                }}>
                                                    {blogs[0] ? blogs[0].description : ""}
                                                </Typography>
                                            </CardContent>
                                            <CardActions>
                                                <Button sx={{ fontSize: "20px" }} endIcon={<ArrowForwardIcon />}
                                                    onClick={() => { if (blogs[0]) { nav(PAGES.ROOT + PAGES.BLOG_LIST + `/${blogs[0].id}`) } }}
                                                >Tìm hiểu thêm </Button>
                                            </CardActions>
                                        </Box>
                                    </Card>
                                </Grid>
                            )
                        }
                        <Grid item xs={12} md={6}>
                            <Grid container m={0} spacing={{ xs: 2, md: 3 }} columns={{ xs: 4, sm: 8, md: 12 }} textAlign={"left"} sx={{ height: "100%" }}>
                                {
                                    blogs && blogs.length > 1 && blogs.map((b, index) => {
                                        if (index > 0) {
                                            return (
                                                <Grid item xs={12} sx={{ height: "33%" }} key={blogs.id}>
                                                    <Card sx={{
                                                        display: 'flex', height: "100%",
                                                        transition: "transform 0.3s ease, box-shadow 0.3s ease",
                                                        '&:hover': {
                                                            cursor: "pointer",
                                                            transform: "scale(1.02) translateY(-10px)",
                                                            boxShadow: "0 8px 16px rgba(0, 0, 0, 0.2)"
                                                        }
                                                    }}>
                                                        <CardMedia
                                                            component="img"
                                                            sx={{ width: 151 }}
                                                            image={b.urlImageDisplay}
                                                            alt="Live from space album cover"
                                                        />
                                                        <Box sx={{ height: "100%", display: "flex", alignItems: "center" }}>
                                                            <CardContent>
                                                                <Typography component="div" variant="h5" sx={{ whiteSpace: "break-spaces", wordBreak: 'break-word' }}>
                                                                    {b.title}
                                                                </Typography>
                                                                <Button sx={{ fontSize: "20px" }} endIcon={<ArrowForwardIcon />}
                                                                    onClick={() => { nav(PAGES.ROOT + PAGES.BLOG_LIST + `/${b.id}`) }}
                                                                >Tìm hiểu thêm </Button>
                                                            </CardContent>
                                                        </Box>
                                                    </Card>
                                                </Grid>
                                            )
                                        }
                                    })
                                }
                            </Grid>
                        </Grid>
                    </Grid>
                </Box>
            </Stack >
        </Box >
    )
}
export default Blog
