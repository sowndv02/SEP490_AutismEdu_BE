import AddCircleOutlineIcon from '@mui/icons-material/AddCircleOutline';
import RemoveIcon from '@mui/icons-material/Remove';
import { FormHelperText, Grid, IconButton, ListItemButton, ListItemIcon, Stack, TextField } from '@mui/material';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Modal from '@mui/material/Modal';
import Typography from '@mui/material/Typography';
import { useFormik } from 'formik';
import { enqueueSnackbar } from 'notistack';
import * as React from 'react';
import ReactQuill from 'react-quill';
import MenuBookIcon from '@mui/icons-material/MenuBook';
import ConfirmDeleteCurriculum from './ConfirmDeleteCurrriculum';
const style = {
    position: 'absolute',
    top: '50%',
    left: '50%',
    transform: 'translate(-50%, -50%)',
    width: 850,
    maxHeight: "90vh",
    bgcolor: 'background.paper',
    boxShadow: 24,
    overflowY: "auto",
    p: 4
};

export default function CurriculumDetail({ curriculum, setCurriculum, endAge, startAge, index, currentCurriculum }) {
    const [open, setOpen] = React.useState(false);
    const [editorContent, setEditorContent] = React.useState(currentCurriculum.description || "");
    const [change, setChange] = React.useState(true);
    const [editorErr, setEditorErr] = React.useState("");
    const [content, setContent] = React.useState("");
    React.useEffect(() => {
        formik.setFieldValue("startAge", currentCurriculum.ageFrom);
        formik.setFieldValue("endAge", currentCurriculum.ageEnd);
        setEditorContent(currentCurriculum.description);
        setContent(currentCurriculum.content || "")
    }, [currentCurriculum])
    const handleOpen = () => {
        if (endAge === "" || startAge === "") {
            enqueueSnackbar("Vui lòng nhập độ tuổi dạy", { variant: "warning" })
        } else
            setOpen(true);
    }
    const handleClose = () => {
        setOpen(false);
    }
    const validate = values => {
        const errors = {};
        if (!values.startAge || !values.endAge) {
            errors.rangeAge = 'Vui lòng nhập độ tuổi';
        } else if (Number(values.startAge) >= Number(values.endAge)) {
            errors.rangeAge = 'Độ tuổi không hợp lệ';
        }
        return errors;
    };

    const toolbarOptions = [
        ['bold', 'italic', 'underline', 'strike'],
        ['blockquote', 'code-block'],
        ['link', 'formula'],
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
    const formik = useFormik({
        initialValues: {
            startAge: currentCurriculum.ageFrom || "",
            endAge: currentCurriculum.ageEnd || ""
        },
        validate,
        onSubmit: (values) => {
            const filterCur = curriculum.filter((c, i) => i !== index);
            const existCur = curriculum.find((c, i) => {
                return c.ageFrom === values.startAge && c.ageEnd === values.endAge
                    && index !== i;
            })
            if (existCur) {
                enqueueSnackbar("Khung chương trình này đã tồn tại!", { variant: "error" })
            }
            else if (editorContent) {
                setCurriculum([{
                    ageFrom: values.startAge,
                    ageEnd: values.endAge,
                    description: editorContent,
                    content: content
                }, ...filterCur
                ])
                handleClose();
                formik.resetForm();
                setEditorContent("");
            }
        }
    });

    React.useEffect(() => {
        if (formik.values.startAge !== currentCurriculum.ageFrom) {
            setChange(false);
            return;
        }
        if (formik.values.endAge !== currentCurriculum.ageEnd) {
            setChange(false);
            return;
        }
        if (editorContent !== currentCurriculum.description) {
            setChange(false);
            return;
        }
        setChange(true);
    }, [formik])

    const handleChangeEdit = (content, delta, source, editor) => {
        const plainText = editor.getText().trim();
        setEditorContent(plainText)
        if (plainText === "") {
            setEditorContent("")
            setEditorErr("Vui lòng nhập khung chương trình!");
            setContent("");
        }
        else {
            if (plainText.length > 2000) setEditorErr("Nội dung quá dài!");
            else setEditorErr("");
            setEditorContent(content);
            setContent(plainText);
        }
    };
    return (
        <Box>
            <ListItemButton>
                <ListItemIcon onClick={handleOpen}>
                    <MenuBookIcon />
                </ListItemIcon>
                <Stack direction="row" sx={{ alignItems: "center", justifyContent: "space-between", flexGrow: 1 }} gap={2} >
                    <Typography onClick={handleOpen}>{currentCurriculum.ageFrom} - {currentCurriculum.ageEnd} tuổi</Typography>
                    <ConfirmDeleteCurriculum curriculum={curriculum} setCurriculum={setCurriculum} index={index} />
                </Stack>
            </ListItemButton>
            <Modal
                open={open}
                onClose={handleClose}
                aria-labelledby="modal-modal-title"
                aria-describedby="modal-modal-description"
            >
                <Box sx={style}>
                    <Typography id="modal-modal-title" variant="h5" component="h2">
                        Thêm khung chương trình
                    </Typography>
                    <form onSubmit={formik.handleSubmit}>
                        <Grid container px="50px" py="50px" columnSpacing={2} rowSpacing={3}>
                            <Grid item xs={2} textAlign="right">Chọn độ tuổi:</Grid>
                            <Grid item xs={10}>
                                <Stack direction='row' alignItems='center' gap={3}>
                                    <TextField size='small' label="Từ" type='number' inputProps={{ min: startAge, max: endAge }}
                                        name='startAge'
                                        value={formik.values.startAge}
                                        onChange={(e) => {
                                            const value = e.target.value;
                                            if (Number.isInteger(Number(value)) || value === '') {
                                                formik.setFieldValue('startAge', value);
                                            }
                                        }}
                                    />
                                    <RemoveIcon />
                                    <TextField size='small' label="Đến" type='number' inputProps={{ min: startAge, max: endAge }}
                                        name='endAge'
                                        value={formik.values.endAge}
                                        onChange={(e) => {
                                            const value = e.target.value;
                                            if (Number.isInteger(Number(value)) || value === '') {
                                                formik.setFieldValue('endAge', value);
                                            }
                                        }} />
                                </Stack>
                                {
                                    formik.errors.rangeAge && (
                                        <FormHelperText error>
                                            {formik.errors.rangeAge}
                                        </FormHelperText>
                                    )
                                }
                            </Grid>
                            <Grid item xs={2} textAlign="right">Nội dung: </Grid>
                            <Grid item xs={10}>
                                <Box mt={2} sx={{ height: "400px", width: "100%" }}>
                                    <ReactQuill
                                        value={editorContent}
                                        onChange={handleChangeEdit}
                                        theme="snow"
                                        modules={{
                                            toolbar: toolbarOptions
                                        }}
                                        style={{ height: "300px" }}
                                    />
                                </Box>
                                <Box textAlign="right" display="flex" sx={{ justifyContent: "space-between" }}>
                                    <Box>
                                        {
                                            editorErr && (
                                                <FormHelperText error>
                                                    {editorErr}
                                                </FormHelperText>
                                            )
                                        }
                                    </Box>
                                    <Typography variant='caption' alignSelf="end">{content.length} / 2000</Typography>
                                </Box>
                            </Grid>

                        </Grid>
                        <Box sx={{ display: "flex", justifyContent: "end", gap: 2 }}>
                            <Button variant='contained' type='submit' disabled={change}>Lưu</Button>
                            <Button onClick={handleClose}>Huỷ</Button>
                        </Box>
                    </form>

                </Box>
            </Modal>
        </Box>
    );
}