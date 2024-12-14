import { Box, Button, FormControl, InputLabel, MenuItem, Select, Stack, TextField, Typography } from '@mui/material';
import { enqueueSnackbar } from 'notistack';
import { useRef, useState } from 'react';
import ReactQuill, { Quill } from 'react-quill';
import { useNavigate } from 'react-router-dom';
import '~/assets/css/texteditor.css';
import UploadImage from '~/components/UploadImage';
import axios from '~/plugins/axiosConfig';
import services from '~/plugins/services';
import PAGES from '~/utils/pages';
import 'react-quill/dist/quill.snow.css'
import ImageResize from 'quill-image-resize-module-react';
import LoadingComponent from '~/components/LoadingComponent';
Quill.register('modules/imageResize', ImageResize);
function BlogCreation() {
    const [status, setStatus] = useState(true);
    const [content, setContent] = useState("");
    const [title, setTitle] = useState("");
    const [image, setImage] = useState(null);
    const nav = useNavigate();
    const quillRef = useRef(null);
    const [loading, setLoading] = useState(false);
    const [description, setDescription] = useState("");
    const [contentText, setContentText] = useState("");
    const handleMouseDown = () => {
        const editor = quillRef.current.getEditor();
        const range = editor.getSelection();
        if (range) {
            setTimeout(() => editor.setSelection(range), 0);
        }
    };
    const toolbarOptions = [
        ['bold', 'italic', 'underline', 'strike'],
        ['blockquote', 'code-block'],
        ['link', 'image', 'video', 'formula'],
        [{ 'header': 1 }, { 'header': 2 }],
        [{ 'list': 'ordered' }, { 'list': 'bullet' }, { 'list': 'check' }],
        [{ 'script': 'sub' }, { 'script': 'super' }],
        [{ 'indent': '-1' }, { 'indent': '+1' }],
        [{ 'direction': 'rtl' }],
        [{ 'size': ['small', false, 'large', 'huge'] }],
        [{ 'header': [1, 2, 3, 4, 5, 6, false] }],
        [{ 'color': [] }, { 'background': [] }],
        [{ 'font': [] }],
        [{ 'align': [] }],
        ['clean']
    ];
    const handleChangeEdit = (content, delta, source, editor) => {
        const plainText = editor.getText().trim();
        setContentText(plainText)
        if (plainText === '') {
            setContent("")
        } else {
            setContent(content)
        }
    };

    const handleSubmit = async () => {
        if (!title) {
            enqueueSnackbar("Bạn chưa nhập tiêu đề", { variant: "error" })
            return;
        }
        else if (title.length < 10) {
            enqueueSnackbar("Tiêu đề phải trên 10 ký tự", { variant: "error" })
            return;
        }
        else if (title.length > 100) {
            enqueueSnackbar("Tiêu đề quá dài", { variant: "error" })
            return;
        }

        if (!description) {
            enqueueSnackbar("Bạn chưa nhập tiêu đề", { variant: "error" })
            return;
        }
        else if (description.length < 100) {
            enqueueSnackbar("Mô tả quá ngắn", { variant: "error" })
            return;
        }
        else if (description.length > 300) {
            enqueueSnackbar("Mô tả quá dài", { variant: "error" })
            return;
        }
        if (!content) {
            enqueueSnackbar("Bạn chưa nhập nội dung", { variant: "error" })
            return;
        }
        else if (contentText.length < 500) {
            enqueueSnackbar("Nội dung quá ngắn", { variant: "error" })
            return;
        }
        else if (contentText.length > 5000) {
            enqueueSnackbar("Nội dung quá dài", { variant: "error" })
            return;
        }
        if (!image) {
            enqueueSnackbar("Bạn chưa upload hình ảnh bìa", { variant: "error" })
            return;
        }
        try {
            setLoading(true);
            const form = new FormData();
            form.append("Title", title);
            form.append("Content", content);
            form.append("Description", description);
            form.append("ImageDisplay", image);
            form.append("IsPublished", status);
            axios.setHeaders({ "Content-Type": "multipart/form-data", "Accept": "application/json, text/plain, multipart/form-data, */*" });
            await services.BlogAPI.createBlog(form, (res) => {
                enqueueSnackbar("Tạo bài viết thành công", { variant: "success" });
                nav(PAGES.BLOG_MANAGEMENT)
            }, (err) => {
                enqueueSnackbar(err.error[0], { variant: "error" })
            })
            axios.setHeaders({ "Content-Type": "application/json", "Accept": "application/json, text/plain, */*" });
        } catch (error) {
            enqueueSnackbar("Tạo bài viết thất bại", { variant: "error" })
        } finally {
            setLoading(true);
        }
    }
    return (
        <Box>
            <Stack direction='row' justifyContent="space-between">
                <Typography variant='h4'>Tạo bài viết mới</Typography>
                <Stack direction='row' gap={3}>
                    <FormControl sx={{ width: "200px", bgcolor: "white" }}>
                        <InputLabel id="status">Trạng thái</InputLabel>
                        <Select
                            labelId="status"
                            value={status}
                            label="Trạng thái"
                            onChange={(e) => setStatus(e.target.value)}
                        >
                            <MenuItem value={true}>Đăng công khai</MenuItem>
                            <MenuItem value={false}>Ẩn tạm thời</MenuItem>
                        </Select>
                    </FormControl>
                    <Button variant='contained' onClick={handleSubmit}>Đăng bài viết</Button>
                </Stack>
            </Stack>
            <Box px="100px">
                <TextField fullWidth sx={{ mt: 3, bgcolor: "white" }} placeholder='Thêm tiêu đề tại đây'
                    value={title} onChange={(e) => setTitle(e.target.value)} />
                <Typography sx={{ textAlign: "right", fontSize: "12px" }}>{title.length} / 100</Typography>
                <Stack direction='row' gap={4} alignItems='center' mt={3}>
                    <Typography fontSize="20px" color='black'>Ảnh bìa</Typography>
                    <UploadImage setImage={setImage} aspectRatio={16 / 9} minDimension={250} />
                </Stack>
                <Box width="100%">
                    {
                        image &&
                        <img src={URL.createObjectURL(image)} alt='avatar' width="100%" />
                    }
                </Box>
                <TextField fullWidth sx={{ mt: 3, bgcolor: "white" }} placeholder='Mô tả bài viết'
                    value={description} onChange={(e) => setDescription(e.target.value)}
                    multiline rows={5} />
                <Typography sx={{ textAlign: "right", fontSize: "12px" }}>{description.length} / 300</Typography>
                <ReactQuill
                    value={content}
                    name="description"
                    onChange={handleChangeEdit}
                    theme="snow"
                    modules={{
                        toolbar: toolbarOptions,
                        clipboard: {
                            matchVisual: false
                        },
                        imageResize: {
                            parchment: Quill.import('parchment'),
                            modules: ['Resize', 'DisplaySize']
                        }
                    }}
                    placeholder='Nhập nội dung bài viết tại đây'
                    style={{ marginTop: "20px" }}
                    onMouseDown={handleMouseDown}
                    ref={quillRef}
                />
                <Typography sx={{ textAlign: "right", fontSize: "12px" }}>{contentText.length}</Typography>
            </Box>
            <LoadingComponent open={loading} />
        </Box>
    )
}

export default BlogCreation
